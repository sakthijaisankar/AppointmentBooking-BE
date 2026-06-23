using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Patients;
using AppValidationException = AppointmentBooking.Application.Exceptions.ValidationException;
using AppointmentBooking.Application.Exceptions;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace AppointmentBooking.Application.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IValidator<CreatePatientProfileRequestDto> _createProfileValidator;
    private readonly IValidator<UpdatePatientProfileRequestDto> _updateProfileValidator;
    private readonly IValidator<CreateEmergencyContactRequestDto> _emergencyContactValidator;
    private readonly IValidator<CreateMedicalHistoryRequestDto> _medicalHistoryValidator;
    private readonly IValidator<UploadDocumentRequestDto> _uploadDocumentValidator;
    private readonly long _maxFileSizeBytes;

    public PatientService(
        IPatientRepository patientRepository,
        IUserRepository userRepository,
        IFileStorageService fileStorageService,
        IValidator<CreatePatientProfileRequestDto> createProfileValidator,
        IValidator<UpdatePatientProfileRequestDto> updateProfileValidator,
        IValidator<CreateEmergencyContactRequestDto> emergencyContactValidator,
        IValidator<CreateMedicalHistoryRequestDto> medicalHistoryValidator,
        IValidator<UploadDocumentRequestDto> uploadDocumentValidator,
        IConfiguration configuration)
    {
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
        _createProfileValidator = createProfileValidator;
        _updateProfileValidator = updateProfileValidator;
        _emergencyContactValidator = emergencyContactValidator;
        _medicalHistoryValidator = medicalHistoryValidator;
        _uploadDocumentValidator = uploadDocumentValidator;
        _maxFileSizeBytes = long.Parse(configuration["FileStorage:MaxFileSizeBytes"] ?? "10485760");
    }

    public async Task<PatientDetailDto> CreateProfileAsync(int userId, CreatePatientProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createProfileValidator, request, cancellationToken);
        await EnsurePatientRoleAsync(userId, cancellationToken);

        if (await _patientRepository.ExistsByUserIdAsync(userId, cancellationToken))
            throw new ConflictException("Patient profile already exists for this user.");

        var patientCode = await _patientRepository.GeneratePatientCodeAsync(cancellationToken);
        var patient = new Patient
        {
            UserId = userId,
            PatientCode = patientCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            BloodGroup = request.BloodGroup,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _patientRepository.CreateAsync(patient, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);

        return MapToDetail(patient);
    }

    public async Task<PatientDetailDto?> GetMyProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepository.GetDetailByUserIdAsync(userId, cancellationToken);
        return patient is null ? null : MapToDetail(patient);
    }

    public async Task<PatientDetailDto> UpdateMyProfileAsync(int userId, UpdatePatientProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateProfileValidator, request, cancellationToken);

        var patient = await _patientRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Patient profile not found. Please create your profile first.");

        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.PhoneNumber = request.PhoneNumber;
        patient.Email = request.Email;
        patient.Address = request.Address;
        patient.BloodGroup = request.BloodGroup;

        await _patientRepository.UpdateAsync(patient, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);

        var detail = await _patientRepository.GetDetailByUserIdAsync(userId, cancellationToken);
        return MapToDetail(detail!);
    }

    public async Task<PatientDetailDto> GetByIdAsync(int patientId, int requestingUserId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        EnsureStaff(roles);
        var patient = await _patientRepository.GetDetailByIdAsync(patientId, cancellationToken)
            ?? throw new NotFoundException($"Patient with ID {patientId} not found.");
        return MapToDetail(patient);
    }

    public async Task<PagedResult<PatientListItemDto>> GetAllAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize is < 1 or > 100) pageSize = 10;

        var (items, total) = await _patientRepository.GetPagedAsync(search, pageNumber, pageSize, cancellationToken);
        return new PagedResult<PatientListItemDto>
        {
            Items = items.Select(MapToListItem),
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<EmergencyContactDto> AddEmergencyContactAsync(
        int userId, IReadOnlyList<string> roles, int? patientId,
        CreateEmergencyContactRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_emergencyContactValidator, request, cancellationToken);
        var patient = await ResolvePatientAsync(userId, roles, patientId, cancellationToken);

        if (request.IsPrimary)
            await ClearPrimaryContactAsync(patient.PatientId, cancellationToken);

        var contact = new EmergencyContact
        {
            PatientId = patient.PatientId,
            ContactName = request.ContactName,
            Relationship = request.Relationship,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            IsPrimary = request.IsPrimary,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _patientRepository.AddEmergencyContactAsync(contact, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);
        return MapContact(contact);
    }

    public async Task<IReadOnlyList<EmergencyContactDto>> GetEmergencyContactsAsync(
        int userId, IReadOnlyList<string> roles, int? patientId, CancellationToken cancellationToken = default)
    {
        var patient = await ResolvePatientAsync(userId, roles, patientId, cancellationToken);
        var contacts = await _patientRepository.GetEmergencyContactsAsync(patient.PatientId, cancellationToken);
        return contacts.Select(MapContact).ToList();
    }

    public async Task<EmergencyContactDto> UpdateEmergencyContactAsync(
        int userId, IReadOnlyList<string> roles, int contactId,
        UpdateEmergencyContactRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_emergencyContactValidator, request, cancellationToken);
        var contact = await _patientRepository.GetEmergencyContactByIdAsync(contactId, cancellationToken)
            ?? throw new NotFoundException("Emergency contact not found.");

        await EnsurePatientAccessAsync(userId, roles, contact.PatientId, cancellationToken);

        if (request.IsPrimary && !contact.IsPrimary)
            await ClearPrimaryContactAsync(contact.PatientId, cancellationToken);

        contact.ContactName = request.ContactName;
        contact.Relationship = request.Relationship;
        contact.PhoneNumber = request.PhoneNumber;
        contact.Email = request.Email;
        contact.IsPrimary = request.IsPrimary;

        await _patientRepository.UpdateEmergencyContactAsync(contact, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);
        return MapContact(contact);
    }

    public async Task DeleteEmergencyContactAsync(int userId, IReadOnlyList<string> roles, int contactId, CancellationToken cancellationToken = default)
    {
        var contact = await _patientRepository.GetEmergencyContactByIdAsync(contactId, cancellationToken)
            ?? throw new NotFoundException("Emergency contact not found.");

        await EnsurePatientAccessAsync(userId, roles, contact.PatientId, cancellationToken);
        await _patientRepository.DeleteEmergencyContactAsync(contact, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PatientMedicalHistoryDto> AddMedicalHistoryAsync(
        int userId, IReadOnlyList<string> roles, int? patientId,
        CreateMedicalHistoryRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_medicalHistoryValidator, request, cancellationToken);
        var patient = await ResolvePatientAsync(userId, roles, patientId, cancellationToken);

        var history = new PatientMedicalHistory
        {
            PatientId = patient.PatientId,
            ConditionName = request.ConditionName,
            DiagnosisDate = request.DiagnosisDate,
            Description = request.Description,
            IsChronic = request.IsChronic,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _patientRepository.AddMedicalHistoryAsync(history, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);
        return MapHistory(history);
    }

    public async Task<IReadOnlyList<PatientMedicalHistoryDto>> GetMedicalHistoryAsync(
        int userId, IReadOnlyList<string> roles, int? patientId, CancellationToken cancellationToken = default)
    {
        var patient = await ResolvePatientAsync(userId, roles, patientId, cancellationToken);
        var items = await _patientRepository.GetMedicalHistoryAsync(patient.PatientId, cancellationToken);
        return items.Select(MapHistory).ToList();
    }

    public async Task<PatientMedicalHistoryDto> UpdateMedicalHistoryAsync(
        int userId, IReadOnlyList<string> roles, int historyId,
        UpdateMedicalHistoryRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_medicalHistoryValidator, request, cancellationToken);
        var history = await _patientRepository.GetMedicalHistoryByIdAsync(historyId, cancellationToken)
            ?? throw new NotFoundException("Medical history record not found.");

        await EnsurePatientAccessAsync(userId, roles, history.PatientId, cancellationToken);

        history.ConditionName = request.ConditionName;
        history.DiagnosisDate = request.DiagnosisDate;
        history.Description = request.Description;
        history.IsChronic = request.IsChronic;

        await _patientRepository.UpdateMedicalHistoryAsync(history, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);
        return MapHistory(history);
    }

    public async Task DeleteMedicalHistoryAsync(int userId, IReadOnlyList<string> roles, int historyId, CancellationToken cancellationToken = default)
    {
        var history = await _patientRepository.GetMedicalHistoryByIdAsync(historyId, cancellationToken)
            ?? throw new NotFoundException("Medical history record not found.");

        await EnsurePatientAccessAsync(userId, roles, history.PatientId, cancellationToken);
        await _patientRepository.DeleteMedicalHistoryAsync(history, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PatientDocumentDto> UploadDocumentAsync(
        int userId, IReadOnlyList<string> roles, int? patientId,
        UploadDocumentRequestDto metadata, Stream fileStream, string fileName, string contentType, long fileSize,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_uploadDocumentValidator, metadata, cancellationToken);

        if (fileSize <= 0 || fileSize > _maxFileSizeBytes)
            throw new AppValidationException($"File size must be between 1 byte and {_maxFileSizeBytes} bytes.");

        var patient = await ResolvePatientAsync(userId, roles, patientId, cancellationToken);
        var (storedFileName, relativePath) = await _fileStorageService.SavePatientDocumentAsync(
            patient.PatientId, fileName, fileStream, cancellationToken);

        var document = new PatientDocument
        {
            PatientId = patient.PatientId,
            DocumentName = metadata.DocumentName,
            DocumentType = metadata.DocumentType,
            StoredFileName = storedFileName,
            FilePath = relativePath,
            ContentType = contentType,
            FileSizeBytes = fileSize,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _patientRepository.AddDocumentAsync(document, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);
        return MapDocument(document);
    }

    public async Task<IReadOnlyList<PatientDocumentDto>> GetDocumentsAsync(
        int userId, IReadOnlyList<string> roles, int? patientId, CancellationToken cancellationToken = default)
    {
        var patient = await ResolvePatientAsync(userId, roles, patientId, cancellationToken);
        var docs = await _patientRepository.GetDocumentsAsync(patient.PatientId, cancellationToken);
        return docs.Select(MapDocument).ToList();
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadDocumentAsync(
        int userId, IReadOnlyList<string> roles, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await _patientRepository.GetDocumentByIdAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Document not found.");

        await EnsurePatientAccessAsync(userId, roles, document.PatientId, cancellationToken);

        var file = await _fileStorageService.GetPatientDocumentAsync(document.FilePath, cancellationToken)
            ?? throw new NotFoundException("Document file not found on storage.");

        return (file.FileStream, file.ContentType, document.DocumentName);
    }

    public async Task DeleteDocumentAsync(int userId, IReadOnlyList<string> roles, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await _patientRepository.GetDocumentByIdAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Document not found.");

        await EnsurePatientAccessAsync(userId, roles, document.PatientId, cancellationToken);
        await _fileStorageService.DeletePatientDocumentAsync(document.FilePath, cancellationToken);
        await _patientRepository.DeleteDocumentAsync(document, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePatientRoleAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("User not found.");

        if (!user.UserRoles.Any(ur => ur.Role.RoleName == AppRoles.Patient))
            throw new UnauthorizedException("Only users with the Patient role can create a patient profile.");
    }

    private static void EnsureStaff(IReadOnlyList<string> roles)
    {
        if (!roles.Any(r => AppRoles.Staff.Contains(r)))
            throw new UnauthorizedException("Staff access required.");
    }

    private async Task<Patient> ResolvePatientAsync(int userId, IReadOnlyList<string> roles, int? patientId, CancellationToken cancellationToken)
    {
        if (IsStaff(roles) && patientId.HasValue)
        {
            return await _patientRepository.GetByIdAsync(patientId.Value, cancellationToken)
                ?? throw new NotFoundException($"Patient with ID {patientId} not found.");
        }

        return await _patientRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Patient profile not found.");
    }

    private async Task EnsurePatientAccessAsync(int userId, IReadOnlyList<string> roles, int patientId, CancellationToken cancellationToken)
    {
        if (IsStaff(roles)) return;

        var own = await _patientRepository.GetByUserIdAsync(userId, cancellationToken);
        if (own is null || own.PatientId != patientId)
            throw new UnauthorizedException("You can only access your own patient records.");
    }

    private static bool IsStaff(IReadOnlyList<string> roles) =>
        roles.Any(r => AppRoles.Staff.Contains(r));

    private async Task ClearPrimaryContactAsync(int patientId, CancellationToken cancellationToken)
    {
        var contacts = await _patientRepository.GetEmergencyContactsAsync(patientId, cancellationToken);
        foreach (var c in contacts.Where(c => c.IsPrimary))
        {
            c.IsPrimary = false;
            await _patientRepository.UpdateEmergencyContactAsync(c, cancellationToken);
        }
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
            throw new AppValidationException("Validation failed.", result.Errors.Select(e => e.ErrorMessage));
    }

    private static PatientDetailDto MapToDetail(Patient p) => new()
    {
        PatientId = p.PatientId,
        UserId = p.UserId,
        PatientCode = p.PatientCode,
        FirstName = p.FirstName,
        LastName = p.LastName,
        FullName = $"{p.FirstName} {p.LastName}",
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender,
        PhoneNumber = p.PhoneNumber,
        Email = p.Email,
        Address = p.Address,
        BloodGroup = p.BloodGroup,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        EmergencyContacts = p.EmergencyContacts?.Select(MapContact).ToList() ?? [],
        MedicalHistory = p.MedicalHistory?.Select(MapHistory).ToList() ?? [],
        Documents = p.Documents?.Select(MapDocument).ToList() ?? []
    };

    private static PatientListItemDto MapToListItem(Patient p) => new()
    {
        PatientId = p.PatientId,
        PatientCode = p.PatientCode,
        FullName = $"{p.FirstName} {p.LastName}",
        Email = p.Email,
        PhoneNumber = p.PhoneNumber,
        DateOfBirth = p.DateOfBirth
    };

    private static EmergencyContactDto MapContact(EmergencyContact c) => new()
    {
        EmergencyContactId = c.EmergencyContactId,
        PatientId = c.PatientId,
        ContactName = c.ContactName,
        Relationship = c.Relationship,
        PhoneNumber = c.PhoneNumber,
        Email = c.Email,
        IsPrimary = c.IsPrimary
    };

    private static PatientMedicalHistoryDto MapHistory(PatientMedicalHistory h) => new()
    {
        PatientMedicalHistoryId = h.PatientMedicalHistoryId,
        PatientId = h.PatientId,
        ConditionName = h.ConditionName,
        DiagnosisDate = h.DiagnosisDate,
        Description = h.Description,
        IsChronic = h.IsChronic
    };

    private static PatientDocumentDto MapDocument(PatientDocument d) => new()
    {
        PatientDocumentId = d.PatientDocumentId,
        PatientId = d.PatientId,
        DocumentName = d.DocumentName,
        DocumentType = d.DocumentType,
        ContentType = d.ContentType,
        FileSizeBytes = d.FileSizeBytes,
        UploadedAt = d.UploadedAt
    };
}
