using AppointmentBooking.Application.DTOs.Appointment;
using FluentValidation;
using System;

namespace AppointmentBooking.Application.Validators;

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequestDto>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .GreaterThan(0)
            .WithMessage("Patient ID is required.");

        RuleFor(x => x.DoctorId)
            .GreaterThan(0)
            .WithMessage("Doctor ID is required.");

        RuleFor(x => x.ClinicId)
            .GreaterThan(0)
            .WithMessage("Clinic ID is required.");

        RuleFor(x => x.ScheduledDateTime)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Scheduled date and time must be in the future.");

        RuleFor(x => x.ReasonForVisit)
            .NotEmpty()
            .WithMessage("Reason for visit is required.")
            .MaximumLength(500)
            .WithMessage("Reason for visit must not exceed 500 characters.");
    }
}

public class UpdateAppointmentStatusRequestValidator : AbstractValidator<UpdateAppointmentStatusRequestDto>
{
    public UpdateAppointmentStatusRequestValidator()
    {
        RuleFor(x => x.StatusName)
            .NotEmpty()
            .WithMessage("Status name is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes must not exceed 500 characters.");
    }
}
