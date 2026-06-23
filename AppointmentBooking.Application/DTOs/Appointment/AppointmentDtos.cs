using System;

namespace AppointmentBooking.Application.DTOs.Appointment;

public record CreateAppointmentRequestDto
{
    public int PatientId { get; init; }
    public int DoctorId { get; init; }
    public int ClinicId { get; init; }
    public DateTime ScheduledDateTime { get; init; }
    public string? ReasonForVisit { get; init; }
}

public record UpdateAppointmentStatusRequestDto
{
    public string StatusName { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

public record AppointmentDetailDto
{
    public int AppointmentId { get; init; }
    public string AppointmentNumber { get; init; } = string.Empty;
    public int PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public int DoctorId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string SpecializationName { get; init; } = string.Empty;
    public int ClinicId { get; init; }
    public string ClinicName { get; init; } = string.Empty;
    public int AppointmentStatusId { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime ScheduledDateTime { get; init; }
    public string? ReasonForVisit { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public int? CurrentPriorityClassificationId { get; init; }
    public string? CurrentPriorityLevelName { get; init; }
    public string? CurrentPriorityColorHex { get; init; }
}

public record AppointmentListItemDto
{
    public int AppointmentId { get; init; }
    public string AppointmentNumber { get; init; } = string.Empty;
    public int PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
    public string SpecializationName { get; init; } = string.Empty;
    public string ClinicName { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public DateTime ScheduledDateTime { get; init; }
    public string? CurrentPriorityLevelName { get; init; }
    public string? CurrentPriorityColorHex { get; init; }
}

public record AvailableSlotDto
{
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
}

public record AppointmentStatusDto
{
    public int AppointmentStatusId { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string? Description { get; init; }
}
