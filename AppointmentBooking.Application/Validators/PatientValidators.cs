using AppointmentBooking.Application.DTOs.Patients;
using FluentValidation;

namespace AppointmentBooking.Application.Validators;

public class CreatePatientProfileRequestValidator : AbstractValidator<CreatePatientProfileRequestDto>
{
    private static readonly string[] AllowedGenders = ["Male", "Female", "Other", "Unknown"];
    private static readonly string[] AllowedBloodGroups = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-", null!];

    public CreatePatientProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Gender)
            .Must(g => AllowedGenders.Contains(g, StringComparer.OrdinalIgnoreCase));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.BloodGroup)
            .Must(bg => bg is null || AllowedBloodGroups.Contains(bg, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Invalid blood group.");
    }
}

public class UpdatePatientProfileRequestValidator : AbstractValidator<UpdatePatientProfileRequestDto>
{
    private static readonly string[] AllowedGenders = ["Male", "Female", "Other", "Unknown"];

    public UpdatePatientProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Gender).Must(g => AllowedGenders.Contains(g, StringComparer.OrdinalIgnoreCase));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public class CreateEmergencyContactRequestValidator : AbstractValidator<CreateEmergencyContactRequestDto>
{
    public CreateEmergencyContactRequestValidator()
    {
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Relationship).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class CreateMedicalHistoryRequestValidator : AbstractValidator<CreateMedicalHistoryRequestDto>
{
    public CreateMedicalHistoryRequestValidator()
    {
        RuleFor(x => x.ConditionName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DiagnosisDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DiagnosisDate.HasValue);
    }
}

public class UploadDocumentRequestValidator : AbstractValidator<UploadDocumentRequestDto>
{
    private static readonly string[] AllowedTypes =
        ["LabReport", "Prescription", "Insurance", "Referral", "Other"];

    public UploadDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.DocumentType)
            .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Document type must be one of: {string.Join(", ", AllowedTypes)}");
    }
}
