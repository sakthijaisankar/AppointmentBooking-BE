using AppointmentBooking.Application.DTOs.Doctors;
using FluentValidation;
using System;

namespace AppointmentBooking.Application.Validators;

public class CreateDoctorProfileRequestValidator : AbstractValidator<CreateDoctorProfileRequestDto>
{
    public CreateDoctorProfileRequestValidator()
    {
        RuleFor(x => x.ClinicId).GreaterThan(0).WithMessage("Clinic ID is required.");
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("User ID is required.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).WithMessage("First name is required (max 100 characters).");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).WithMessage("Last name is required (max 100 characters).");
        RuleFor(x => x.SpecializationId).GreaterThan(0).WithMessage("Specialization is required.");
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50).WithMessage("License number is required (max 50 characters).");
    }
}

public class UpdateDoctorProfileRequestValidator : AbstractValidator<UpdateDoctorProfileRequestDto>
{
    public UpdateDoctorProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SpecializationId).GreaterThan(0);
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
    }
}

public class CreateDoctorScheduleRequestValidator : AbstractValidator<CreateDoctorScheduleRequestDto>
{
    public CreateDoctorScheduleRequestValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(0, 6)
            .WithMessage("Day of week must be between 0 (Sunday) and 6 (Saturday).");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .Must(BeAValidTime)
            .WithMessage("Start time must be in a valid time format (e.g. HH:mm).");

        RuleFor(x => x.EndTime)
            .NotEmpty()
            .Must(BeAValidTime)
            .WithMessage("End time must be in a valid time format (e.g. HH:mm).")
            .Must((dto, endTime) => BeAfterStartTime(dto.StartTime, endTime))
            .WithMessage("End time must be after start time.");

        RuleFor(x => x.SlotDurationMinutes)
            .InclusiveBetween(5, 120)
            .WithMessage("Slot duration must be between 5 and 120 minutes.");
    }

    private bool BeAValidTime(string time)
    {
        return TimeSpan.TryParse(time, out _);
    }

    private bool BeAfterStartTime(string startTimeStr, string endTimeStr)
    {
        if (TimeSpan.TryParse(startTimeStr, out var startTime) && TimeSpan.TryParse(endTimeStr, out var endTime))
        {
            return endTime > startTime;
        }
        return false;
    }
}

public class CreateSpecializationRequestValidator : AbstractValidator<CreateSpecializationRequestDto>
{
    public CreateSpecializationRequestValidator()
    {
        RuleFor(x => x.SpecializationName)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Specialization name is required (max 100 characters).");
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");
    }
}

public class UpdateSpecializationRequestValidator : AbstractValidator<UpdateSpecializationRequestDto>
{
    public UpdateSpecializationRequestValidator()
    {
        RuleFor(x => x.SpecializationName)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
