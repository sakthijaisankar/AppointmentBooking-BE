using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.Consultation;
using AppointmentBooking.Application.Exceptions;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;

namespace AppointmentBooking.Application.Services;

public class ConsultationService : IConsultationService
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly INotificationService _notificationService;

    public ConsultationService(
        IConsultationRepository consultationRepository,
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        INotificationService notificationService)
    {
        _consultationRepository = consultationRepository;
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _notificationService = notificationService;
    }

    public async Task<ConsultationDto> CreateConsultationAsync(
        CreateConsultationRequestDto request,
        int consultedByUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate appointment exists
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException($"Appointment {request.AppointmentId} not found.");

        // 2. Validate appointment status (must be InProgress or Completed)
        var statusName = appointment.AppointmentStatus.StatusName;
        if (statusName != "InProgress" && statusName != "Completed")
            throw new ValidationException("Consultation can only be created for appointments that are InProgress or Completed.");

        // 3. Prevent duplicate consultation
        var existing = await _consultationRepository.GetByAppointmentIdAsync(request.AppointmentId, cancellationToken);
        if (existing != null)
            throw new ConflictException("A consultation already exists for this appointment.");

        // 4. Build entity
        var consultation = new Consultation
        {
            AppointmentId = request.AppointmentId,
            DoctorId = appointment.DoctorId,
            PatientId = appointment.PatientId,
            Diagnosis = request.Diagnosis,
            ClinicalNotes = request.ClinicalNotes,
            FollowUpRequired = request.FollowUpRequired,
            FollowUpDate = request.FollowUpDate,
            ConsultedByUserId = consultedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        // 5. Add prescriptions
        if (request.Prescriptions != null)
        {
            foreach (var p in request.Prescriptions)
            {
                consultation.Prescriptions.Add(new Prescription
                {
                    MedicineName = p.MedicineName,
                    Dosage = p.Dosage,
                    Frequency = p.Frequency,
                    DurationDays = p.DurationDays,
                    Instructions = p.Instructions,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _consultationRepository.AddAsync(consultation, cancellationToken);
        await _consultationRepository.SaveChangesAsync(cancellationToken);

        // 6. Reload fully to return
        var reloaded = await _consultationRepository.GetByIdAsync(consultation.ConsultationId, cancellationToken)
            ?? throw new NotFoundException("Failed to reload consultation after save.");

        try
        {
            var pat = await _patientRepository.GetByIdAsync(reloaded.PatientId, cancellationToken);
            var doc = await _doctorRepository.GetByIdAsync(reloaded.DoctorId, cancellationToken);
            if (pat != null && doc != null)
            {
                await _notificationService.SendNotificationAsync(
                    "CONSULTATION_COMPLETED",
                    pat.UserId,
                    reloaded.AppointmentId,
                    new Dictionary<string, string>
                    {
                        { "PatientName", $"{pat.FirstName} {pat.LastName}" },
                        { "DoctorName", $"{doc.FirstName} {doc.LastName}" },
                        { "AppointmentNumber", reloaded.Appointment.AppointmentNumber },
                        { "Diagnosis", reloaded.Diagnosis }
                    },
                    cancellationToken
                );
            }
        }
        catch
        {
            // Fail-safe
        }

        return MapToDto(reloaded);
    }

    public async Task<ConsultationDto?> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        var consultation = await _consultationRepository.GetByAppointmentIdAsync(appointmentId, cancellationToken);
        return consultation == null ? null : MapToDto(consultation);
    }

    public async Task<ConsultationDto> GetByIdAsync(int consultationId, CancellationToken cancellationToken = default)
    {
        var consultation = await _consultationRepository.GetByIdAsync(consultationId, cancellationToken)
            ?? throw new NotFoundException($"Consultation {consultationId} not found.");
        return MapToDto(consultation);
    }

    public async Task<PagedResult<ConsultationSummaryDto>> GetPatientHistoryAsync(int patientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _consultationRepository.GetByPatientIdAsync(patientId, page, pageSize, cancellationToken);
        return new PagedResult<ConsultationSummaryDto>
        {
            Items = items.Select(MapToSummaryDto).ToList(),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<ConsultationSummaryDto>> GetDoctorConsultationsAsync(int doctorId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _consultationRepository.GetByDoctorIdAsync(doctorId, page, pageSize, cancellationToken);
        return new PagedResult<ConsultationSummaryDto>
        {
            Items = items.Select(MapToSummaryDto).ToList(),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<ConsultationDto> UpdateConsultationAsync(
        int consultationId,
        UpdateConsultationRequestDto request,
        int updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var consultation = await _consultationRepository.GetByIdAsync(consultationId, cancellationToken)
            ?? throw new NotFoundException($"Consultation {consultationId} not found.");

        consultation.Diagnosis = request.Diagnosis;
        consultation.ClinicalNotes = request.ClinicalNotes;
        consultation.FollowUpRequired = request.FollowUpRequired;
        consultation.FollowUpDate = request.FollowUpDate;
        consultation.UpdatedAt = DateTime.UtcNow;

        await _consultationRepository.SaveChangesAsync(cancellationToken);

        var reloaded = await _consultationRepository.GetByIdAsync(consultationId, cancellationToken)!;
        return MapToDto(reloaded!);
    }

    public async Task<ConsultationDto> AddPrescriptionAsync(
        int consultationId,
        AddPrescriptionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var consultation = await _consultationRepository.GetByIdAsync(consultationId, cancellationToken)
            ?? throw new NotFoundException($"Consultation {consultationId} not found.");

        var prescription = new Prescription
        {
            ConsultationId = consultationId,
            MedicineName = request.MedicineName,
            Dosage = request.Dosage,
            Frequency = request.Frequency,
            DurationDays = request.DurationDays,
            Instructions = request.Instructions,
            CreatedAt = DateTime.UtcNow
        };

        await _consultationRepository.AddPrescriptionAsync(prescription, cancellationToken);
        await _consultationRepository.SaveChangesAsync(cancellationToken);

        var reloaded = await _consultationRepository.GetByIdAsync(consultationId, cancellationToken)!;
        return MapToDto(reloaded!);
    }

    public async Task<ConsultationDto> DeletePrescriptionAsync(
        int consultationId,
        int prescriptionId,
        CancellationToken cancellationToken = default)
    {
        var prescription = await _consultationRepository.GetPrescriptionByIdAsync(prescriptionId, cancellationToken)
            ?? throw new NotFoundException($"Prescription {prescriptionId} not found.");

        if (prescription.ConsultationId != consultationId)
            throw new ValidationException("Prescription does not belong to this consultation.");

        await _consultationRepository.RemovePrescriptionAsync(prescription, cancellationToken);
        await _consultationRepository.SaveChangesAsync(cancellationToken);

        var reloaded = await _consultationRepository.GetByIdAsync(consultationId, cancellationToken)!;
        return MapToDto(reloaded!);
    }

    // ─── Mappers ─────────────────────────────────────────────────────────────

    private static ConsultationDto MapToDto(Consultation c) => new(
        ConsultationId: c.ConsultationId,
        AppointmentId: c.AppointmentId,
        AppointmentNumber: c.Appointment.AppointmentNumber,
        PatientId: c.PatientId,
        PatientName: $"{c.Patient.FirstName} {c.Patient.LastName}",
        PatientCode: c.Patient.PatientCode,
        DoctorId: c.DoctorId,
        DoctorName: $"Dr. {c.Doctor.FirstName} {c.Doctor.LastName}",
        SpecializationName: c.Doctor.Specialization?.SpecializationName ?? string.Empty,
        Diagnosis: c.Diagnosis,
        ClinicalNotes: c.ClinicalNotes,
        FollowUpRequired: c.FollowUpRequired,
        FollowUpDate: c.FollowUpDate,
        AppointmentStatusName: c.Appointment.AppointmentStatus?.StatusName,
        ScheduledDateTime: c.Appointment.ScheduledDateTime,
        ConsultedByName: c.ConsultedByUser?.FullName,
        Prescriptions: c.Prescriptions.Select(p => new PrescriptionDto(
            PrescriptionId: p.PrescriptionId,
            ConsultationId: p.ConsultationId,
            MedicineName: p.MedicineName,
            Dosage: p.Dosage,
            Frequency: p.Frequency,
            DurationDays: p.DurationDays,
            Instructions: p.Instructions,
            CreatedAt: p.CreatedAt
        )).ToList(),
        CreatedAt: c.CreatedAt,
        UpdatedAt: c.UpdatedAt
    );

    private static ConsultationSummaryDto MapToSummaryDto(Consultation c) => new(
        ConsultationId: c.ConsultationId,
        AppointmentId: c.AppointmentId,
        AppointmentNumber: c.Appointment.AppointmentNumber,
        PatientName: $"{c.Patient.FirstName} {c.Patient.LastName}",
        PatientCode: c.Patient.PatientCode,
        DoctorName: $"Dr. {c.Doctor.FirstName} {c.Doctor.LastName}",
        Diagnosis: c.Diagnosis,
        FollowUpRequired: c.FollowUpRequired,
        FollowUpDate: c.FollowUpDate,
        PrescriptionCount: c.Prescriptions.Count,
        CreatedAt: c.CreatedAt
    );
}
