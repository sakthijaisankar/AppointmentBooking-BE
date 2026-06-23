using System;
using System.Collections.Generic;

namespace AppointmentBooking.Application.DTOs.Doctors;

public record CreateDoctorProfileRequestDto
{
    public int ClinicId { get; init; }
    public int UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int SpecializationId { get; init; }
    public string LicenseNumber { get; init; } = string.Empty;
}

public record UpdateDoctorProfileRequestDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int SpecializationId { get; init; }
    public string LicenseNumber { get; init; } = string.Empty;
}

public record DoctorProfileDto
{
    public int DoctorId { get; init; }
    public int ClinicId { get; init; }
    public int? UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public int SpecializationId { get; init; }
    public string SpecializationName { get; init; } = string.Empty;
    public string LicenseNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record DoctorDetailDto : DoctorProfileDto
{
    public string ClinicName { get; init; } = string.Empty;
    public IReadOnlyList<DoctorScheduleDto> Schedules { get; init; } = Array.Empty<DoctorScheduleDto>();
}

public record CreateDoctorScheduleRequestDto
{
    public int DayOfWeek { get; init; } // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
    public string StartTime { get; init; } = string.Empty; // Format "HH:mm" or "HH:mm:ss"
    public string EndTime { get; init; } = string.Empty; // Format "HH:mm" or "HH:mm:ss"
    public int SlotDurationMinutes { get; init; } = 15;
}

public record UpdateDoctorScheduleRequestDto : CreateDoctorScheduleRequestDto;

public record DoctorScheduleDto
{
    public int DoctorScheduleId { get; init; }
    public int DoctorId { get; init; }
    public int DayOfWeek { get; init; }
    public string DayName { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public int SlotDurationMinutes { get; init; }
}

public record SpecializationDto
{
    public int SpecializationId { get; init; }
    public string SpecializationName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public record CreateSpecializationRequestDto
{
    public string SpecializationName { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record UpdateSpecializationRequestDto : CreateSpecializationRequestDto
{
    public bool IsActive { get; init; }
}
