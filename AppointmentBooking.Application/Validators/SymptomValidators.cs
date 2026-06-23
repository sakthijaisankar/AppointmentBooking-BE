using AppointmentBooking.Application.DTOs.Symptom;
using FluentValidation;

namespace AppointmentBooking.Application.Validators;

public class SymptomSubmissionItemValidator : AbstractValidator<SymptomSubmissionItemDto>
{
    public SymptomSubmissionItemValidator()
    {
        RuleFor(x => x.SymptomId)
            .GreaterThan(0)
            .WithMessage("Symptom ID is required.");

        RuleFor(x => x.SeverityLevel)
            .InclusiveBetween(1, 10)
            .WithMessage("Severity level must be between 1 and 10.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Symptom notes must not exceed 500 characters.");
    }
}

public class SubmitSymptomsRequestValidator : AbstractValidator<SubmitSymptomsRequestDto>
{
    public SubmitSymptomsRequestValidator()
    {
        RuleFor(x => x.AppointmentId)
            .GreaterThan(0)
            .WithMessage("Appointment ID is required.");

        RuleFor(x => x.ExistingConditions)
            .MaximumLength(1000)
            .WithMessage("Existing conditions must not exceed 1000 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("General notes must not exceed 1000 characters.");

        RuleFor(x => x.Symptoms)
            .NotEmpty()
            .WithMessage("At least one symptom must be submitted.");

        RuleForEach(x => x.Symptoms)
            .SetValidator(new SymptomSubmissionItemValidator());
    }
}
