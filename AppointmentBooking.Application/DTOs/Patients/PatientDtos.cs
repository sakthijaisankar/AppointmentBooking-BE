namespace AppointmentBooking.Application.DTOs.Patients;

public record CreatePatientProfileRequestDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? BloodGroup { get; init; }
}

public record UpdatePatientProfileRequestDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? BloodGroup { get; init; }
}

public record PatientProfileDto
{
    public int PatientId { get; init; }
    public int UserId { get; init; }
    public string PatientCode { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? BloodGroup { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record PatientDetailDto : PatientProfileDto
{
    public IReadOnlyList<EmergencyContactDto> EmergencyContacts { get; init; } = Array.Empty<EmergencyContactDto>();
    public IReadOnlyList<PatientMedicalHistoryDto> MedicalHistory { get; init; } = Array.Empty<PatientMedicalHistoryDto>();
    public IReadOnlyList<PatientDocumentDto> Documents { get; init; } = Array.Empty<PatientDocumentDto>();
}

public record CreateEmergencyContactRequestDto
{
    public string ContactName { get; init; } = string.Empty;
    public string Relationship { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsPrimary { get; init; }
}

public record UpdateEmergencyContactRequestDto : CreateEmergencyContactRequestDto;

public record EmergencyContactDto
{
    public int EmergencyContactId { get; init; }
    public int PatientId { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string Relationship { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsPrimary { get; init; }
}

public record CreateMedicalHistoryRequestDto
{
    public string ConditionName { get; init; } = string.Empty;
    public DateOnly? DiagnosisDate { get; init; }
    public string? Description { get; init; }
    public bool IsChronic { get; init; }
}

public record UpdateMedicalHistoryRequestDto : CreateMedicalHistoryRequestDto;

public record PatientMedicalHistoryDto
{
    public int PatientMedicalHistoryId { get; init; }
    public int PatientId { get; init; }
    public string ConditionName { get; init; } = string.Empty;
    public DateOnly? DiagnosisDate { get; init; }
    public string? Description { get; init; }
    public bool IsChronic { get; init; }
}

public record PatientDocumentDto
{
    public int PatientDocumentId { get; init; }
    public int PatientId { get; init; }
    public string DocumentName { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
}

public record UploadDocumentRequestDto
{
    public string DocumentName { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
}

public record PatientListItemDto
{
    public int PatientId { get; init; }
    public string PatientCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public DateOnly DateOfBirth { get; init; }
}
