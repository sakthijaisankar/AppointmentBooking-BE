using AppointmentBooking.Application.DTOs.PatientPriority;
using FluentValidation;

namespace AppointmentBooking.Application.Validators;

public class ClassifyPatientPriorityRequestValidator : AbstractValidator<ClassifyPatientPriorityRequestDto>
{
    private static readonly string[] AllowedGenders = ["Male", "Female", "Other", "Unknown"];

    public ClassifyPatientPriorityRequestValidator()
    {
        RuleFor(x => x.Age)
            .InclusiveBetween(0, 150)
            .WithMessage("Age must be between 0 and 150.");

        RuleFor(x => x.Gender)
            .NotEmpty()
            .Must(g => AllowedGenders.Contains(g, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Gender must be one of: {string.Join(", ", AllowedGenders)}.");

        RuleFor(x => x.HeartRate)
            .InclusiveBetween(30, 250)
            .When(x => x.HeartRate.HasValue)
            .WithMessage("Heart rate must be between 30 and 250 bpm.");

        RuleFor(x => x.BloodPressureSystolic)
            .InclusiveBetween(60, 250)
            .When(x => x.BloodPressureSystolic.HasValue);

        RuleFor(x => x.BloodPressureDiastolic)
            .InclusiveBetween(40, 150)
            .When(x => x.BloodPressureDiastolic.HasValue);

        RuleFor(x => x.TemperatureCelsius)
            .InclusiveBetween(35.0m, 42.0m)
            .When(x => x.TemperatureCelsius.HasValue);

        RuleFor(x => x.OxygenSaturation)
            .InclusiveBetween(50.0m, 100.0m)
            .When(x => x.OxygenSaturation.HasValue);

        RuleFor(x => x.PainLevel)
            .InclusiveBetween(0, 10)
            .When(x => x.PainLevel.HasValue);

        RuleFor(x => x.SymptomSeverityScore)
            .InclusiveBetween(0, 10)
            .When(x => x.SymptomSeverityScore.HasValue);

        RuleFor(x => x.PrimarySymptoms)
            .MaximumLength(1000);

        RuleFor(x => x.Comorbidities)
            .MaximumLength(1000);
    }
}

public class OverridePatientPriorityRequestValidator : AbstractValidator<OverridePatientPriorityRequestDto>
{
    public OverridePatientPriorityRequestValidator()
    {
        RuleFor(x => x.OverridePriorityLevelId)
            .GreaterThan(0);

        RuleFor(x => x.OverrideReason)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(500)
            .WithMessage("Override reason must be between 10 and 500 characters.");
    }
}
