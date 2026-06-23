using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.PatientPriority;
using AppValidationException = AppointmentBooking.Application.Exceptions.ValidationException;
using AppointmentBooking.Application.Exceptions;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Application.Interfaces.ML;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AppointmentBooking.Application.Services;

public class PatientPriorityService : IPatientPriorityService
{
    private readonly IPatientPriorityRepository _repository;
    private readonly IPatientPriorityPredictionEngine _predictionEngine;
    private readonly IValidator<ClassifyPatientPriorityRequestDto> _classifyValidator;
    private readonly IValidator<OverridePatientPriorityRequestDto> _overrideValidator;
    private readonly ILogger<PatientPriorityService> _logger;

    public PatientPriorityService(
        IPatientPriorityRepository repository,
        IPatientPriorityPredictionEngine predictionEngine,
        IValidator<ClassifyPatientPriorityRequestDto> classifyValidator,
        IValidator<OverridePatientPriorityRequestDto> overrideValidator,
        ILogger<PatientPriorityService> logger)
    {
        _repository = repository;
        _predictionEngine = predictionEngine;
        _classifyValidator = classifyValidator;
        _overrideValidator = overrideValidator;
        _logger = logger;
    }

    public async Task<PatientPriorityClassificationDto> ClassifyPatientAsync(
        int patientId,
        ClassifyPatientPriorityRequestDto request,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_classifyValidator, request, cancellationToken);

        var patient = await _repository.GetPatientByIdAsync(patientId, cancellationToken)
            ?? throw new NotFoundException($"Patient with ID {patientId} was not found.");

        if (request.AppointmentId.HasValue)
        {
            var appointment = await _repository.GetAppointmentByIdAsync(request.AppointmentId.Value, cancellationToken)
                ?? throw new NotFoundException($"Appointment with ID {request.AppointmentId} was not found.");

            if (appointment.PatientId != patientId)
                throw new AppValidationException("Appointment does not belong to the specified patient.");
        }

        var modelVersion = await _repository.GetActiveModelVersionAsync(cancellationToken)
            ?? throw new ConflictException("No active ML model version is configured.");

        var prediction = _predictionEngine.Predict(request);

        var priorityLevel = await _repository.GetPriorityLevelByCodeAsync(prediction.PredictedLevelCode, cancellationToken)
            ?? throw new ConflictException($"Priority level '{prediction.PredictedLevelCode}' is not configured.");

        var clinicalFeatures = new PatientClinicalFeature
        {
            PatientId = patientId,
            AppointmentId = request.AppointmentId,
            Age = request.Age,
            Gender = request.Gender,
            HeartRate = request.HeartRate,
            BloodPressureSystolic = request.BloodPressureSystolic,
            BloodPressureDiastolic = request.BloodPressureDiastolic,
            TemperatureCelsius = request.TemperatureCelsius,
            OxygenSaturation = request.OxygenSaturation,
            PainLevel = request.PainLevel,
            SymptomSeverityScore = request.SymptomSeverityScore,
            HasChronicCondition = request.HasChronicCondition,
            HasRecentHospitalization = request.HasRecentHospitalization,
            PrimarySymptoms = request.PrimarySymptoms,
            Comorbidities = request.Comorbidities,
            CapturedAt = DateTime.UtcNow,
            CapturedByUserId = userId
        };

        await _repository.AddClinicalFeaturesAsync(clinicalFeatures, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var classification = new PatientPriorityClassification
        {
            PatientId = patientId,
            PatientClinicalFeatureId = clinicalFeatures.PatientClinicalFeatureId,
            MlModelVersionId = modelVersion.MlModelVersionId,
            PredictedPriorityLevelId = priorityLevel.PriorityLevelId,
            ConfidenceScore = prediction.ConfidenceScore,
            RiskScore = prediction.RiskScore,
            ClassificationReason = prediction.ClassificationReason,
            InputFeaturesJson = prediction.InputFeaturesJson,
            IsCurrent = true,
            ClassifiedAt = DateTime.UtcNow,
            ClassifiedByUserId = userId
        };

        await _repository.AddClassificationAsync(classification, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _repository.SetCurrentClassificationAsync(patientId, classification.PatientPriorityClassificationId, cancellationToken);

        if (request.AppointmentId.HasValue)
            await _repository.LinkAppointmentToClassificationAsync(request.AppointmentId.Value, classification.PatientPriorityClassificationId, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Patient {PatientId} classified as {PriorityLevel} with confidence {Confidence}",
            patientId, priorityLevel.LevelCode, prediction.ConfidenceScore);

        classification.Patient = patient;
        classification.PredictedPriorityLevel = priorityLevel;
        classification.MlModelVersion = modelVersion;
        classification.PatientClinicalFeature = clinicalFeatures;

        return MapToDto(classification);
    }

    public async Task<PatientPriorityClassificationDto?> GetCurrentClassificationAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await _repository.GetPatientByIdAsync(patientId, cancellationToken)
            ?? throw new NotFoundException($"Patient with ID {patientId} was not found.");

        var classification = await _repository.GetCurrentClassificationAsync(patientId, cancellationToken);
        return classification is null ? null : MapToDto(classification);
    }

    public async Task<PagedResult<PatientPriorityClassificationDto>> GetClassificationHistoryAsync(
        int patientId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize is < 1 or > 100) pageSize = 10;

        var patient = await _repository.GetPatientByIdAsync(patientId, cancellationToken)
            ?? throw new NotFoundException($"Patient with ID {patientId} was not found.");

        var items = await _repository.GetClassificationHistoryAsync(patientId, pageNumber, pageSize, cancellationToken);
        var total = await _repository.GetClassificationHistoryCountAsync(patientId, cancellationToken);

        return new PagedResult<PatientPriorityClassificationDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PatientPriorityClassificationDto> OverrideClassificationAsync(
        int classificationId,
        OverridePatientPriorityRequestDto request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_overrideValidator, request, cancellationToken);

        var classification = await _repository.GetClassificationByIdAsync(classificationId, cancellationToken)
            ?? throw new NotFoundException($"Classification with ID {classificationId} was not found.");

        if (!classification.IsCurrent)
            throw new ConflictException("Only the current classification can be overridden.");

        var overrideLevel = await _repository.GetPriorityLevelByIdAsync(request.OverridePriorityLevelId, cancellationToken)
            ?? throw new NotFoundException($"Priority level with ID {request.OverridePriorityLevelId} was not found.");

        if (classification.PredictedPriorityLevelId == request.OverridePriorityLevelId)
            throw new AppValidationException("Override priority level must differ from the predicted level.");

        var overrideRecord = new PriorityClassificationOverride
        {
            PatientPriorityClassificationId = classificationId,
            OriginalPriorityLevelId = classification.PredictedPriorityLevelId,
            OverridePriorityLevelId = request.OverridePriorityLevelId,
            OverrideReason = request.OverrideReason,
            OverriddenByUserId = userId,
            OverriddenAt = DateTime.UtcNow
        };

        await _repository.AddOverrideAsync(overrideRecord, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        classification = await _repository.GetClassificationByIdAsync(classificationId, cancellationToken)
            ?? throw new NotFoundException($"Classification with ID {classificationId} was not found.");

        return MapToDto(classification);
    }

    public async Task<IReadOnlyList<PriorityLevelDto>> GetPriorityLevelsAsync(CancellationToken cancellationToken = default)
    {
        var levels = await _repository.GetActivePriorityLevelsAsync(cancellationToken);
        return levels.Select(MapLevelToDto).ToList();
    }

    public async Task<MlModelVersionDto?> GetActiveModelVersionAsync(CancellationToken cancellationToken = default)
    {
        var model = await _repository.GetActiveModelVersionAsync(cancellationToken);
        if (model is null) return null;

        return new MlModelVersionDto
        {
            MlModelVersionId = model.MlModelVersionId,
            ModelName = model.ModelName,
            VersionNumber = model.VersionNumber,
            AlgorithmType = model.AlgorithmType,
            AccuracyScore = model.AccuracyScore,
            IsActive = model.IsActive,
            DeployedAt = model.DeployedAt
        };
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
            throw new AppValidationException("Validation failed.", result.Errors.Select(e => e.ErrorMessage));
    }

    private static PatientPriorityClassificationDto MapToDto(PatientPriorityClassification entity)
    {
        var latestOverride = entity.Overrides.OrderByDescending(o => o.OverriddenAt).FirstOrDefault();

        return new PatientPriorityClassificationDto
        {
            PatientPriorityClassificationId = entity.PatientPriorityClassificationId,
            PatientId = entity.PatientId,
            PatientName = $"{entity.Patient.FirstName} {entity.Patient.LastName}",
            PredictedPriorityLevel = MapLevelToDto(entity.PredictedPriorityLevel),
            EffectivePriorityLevel = latestOverride is not null
                ? MapLevelToDto(latestOverride.OverridePriorityLevel)
                : MapLevelToDto(entity.PredictedPriorityLevel),
            ConfidenceScore = entity.ConfidenceScore,
            RiskScore = entity.RiskScore,
            ClassificationReason = entity.ClassificationReason,
            ModelVersion = entity.MlModelVersion.VersionNumber,
            IsCurrent = entity.IsCurrent,
            IsOverridden = latestOverride is not null,
            OverrideReason = latestOverride?.OverrideReason,
            ClassifiedAt = entity.ClassifiedAt,
            ClinicalFeatures = MapClinicalFeatures(entity.PatientClinicalFeature)
        };
    }

    private static ClassifyPatientPriorityRequestDto MapClinicalFeatures(PatientClinicalFeature f) => new()
    {
        AppointmentId = f.AppointmentId,
        Age = f.Age,
        Gender = f.Gender,
        HeartRate = f.HeartRate,
        BloodPressureSystolic = f.BloodPressureSystolic,
        BloodPressureDiastolic = f.BloodPressureDiastolic,
        TemperatureCelsius = f.TemperatureCelsius,
        OxygenSaturation = f.OxygenSaturation,
        PainLevel = f.PainLevel,
        SymptomSeverityScore = f.SymptomSeverityScore,
        HasChronicCondition = f.HasChronicCondition,
        HasRecentHospitalization = f.HasRecentHospitalization,
        PrimarySymptoms = f.PrimarySymptoms,
        Comorbidities = f.Comorbidities
    };

    private static PriorityLevelDto MapLevelToDto(PriorityLevel level) => new()
    {
        PriorityLevelId = level.PriorityLevelId,
        LevelCode = level.LevelCode,
        LevelName = level.LevelName,
        SortOrder = level.SortOrder,
        ColorHex = level.ColorHex,
        Description = level.Description
    };
}
