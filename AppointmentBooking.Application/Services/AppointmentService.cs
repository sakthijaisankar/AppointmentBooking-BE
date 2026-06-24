using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Appointment;
using AppointmentBooking.Application.Exceptions;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppValidationException = AppointmentBooking.Application.Exceptions.ValidationException;

namespace AppointmentBooking.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IValidator<CreateAppointmentRequestDto> _createAppointmentValidator;
    private readonly IValidator<UpdateAppointmentStatusRequestDto> _updateStatusValidator;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository,
        IPatientRepository patientRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IValidator<CreateAppointmentRequestDto> createAppointmentValidator,
        IValidator<UpdateAppointmentStatusRequestDto> updateStatusValidator)
    {
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _createAppointmentValidator = createAppointmentValidator;
        _updateStatusValidator = updateStatusValidator;
    }

    public async Task<AppointmentDetailDto> BookAppointmentAsync(
        CreateAppointmentRequestDto request, 
        int? createdByUserId, 
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createAppointmentValidator, request, cancellationToken);

        // Verify patient
        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException("Patient not found.");

        // Verify doctor
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId, cancellationToken)
            ?? throw new NotFoundException("Doctor not found.");

        // Fetch doctor schedule for the day of week of the appointment date
        var date = DateOnly.FromDateTime(request.ScheduledDateTime);
        var schedules = await _doctorRepository.GetSchedulesByDoctorIdAsync(request.DoctorId, cancellationToken);
        var dayOfWeek = (int)request.ScheduledDateTime.DayOfWeek;
        
        var daySchedule = schedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek && s.IsActive)
            ?? throw new ConflictException("Doctor does not work on this day of the week.");

        var appTime = request.ScheduledDateTime.TimeOfDay;
        var slotDuration = TimeSpan.FromMinutes(daySchedule.SlotDurationMinutes);

        // Check if the scheduled time falls within the doctor's shift
        if (appTime < daySchedule.StartTime || appTime + slotDuration > daySchedule.EndTime)
        {
            throw new ConflictException("Selected time slot is outside the doctor's working hours.");
        }

        // Verify time slot starts on a valid interval
        var timeOffset = appTime - daySchedule.StartTime;
        if (timeOffset.Ticks % slotDuration.Ticks != 0)
        {
            throw new ConflictException("Selected time is not a valid slot interval.");
        }

        // Check for conflict with existing appointments on that date
        var existingAppointments = await _appointmentRepository.GetAppointmentsByDoctorAndDateAsync(request.DoctorId, date, cancellationToken);
        var overlaps = existingAppointments.Any(a =>
        {
            var appStart = a.ScheduledDateTime.TimeOfDay;
            var appEnd = appStart + slotDuration;
            var reqStart = appTime;
            var reqEnd = appTime + slotDuration;
            return reqStart < appEnd && appStart < reqEnd;
        });

        if (overlaps)
        {
            throw new ConflictException("The selected time slot is already booked.");
        }

        // Determine default status: Confirmed for Staff bookings, Pending for Patients self-booking
        var defaultStatusName = "Pending";
        if (createdByUserId.HasValue)
        {
            var createdByUser = await _userRepository.GetByIdAsync(createdByUserId.Value, cancellationToken);
            if (createdByUser != null)
            {
                var isStaff = createdByUser.UserRoles.Any(ur => AppRoles.Staff.Contains(ur.Role.RoleName));
                if (isStaff)
                {
                    defaultStatusName = "Confirmed";
                }
            }
        }

        var status = await _appointmentRepository.GetStatusByNameAsync(defaultStatusName, cancellationToken)
            ?? throw new NotFoundException($"Appointment status '{defaultStatusName}' not found.");

        // Generate unique appointment number: APT-YYYYMMDD-XXXX
        var todayStr = request.ScheduledDateTime.ToString("yyyyMMdd");
        var countToday = await _appointmentRepository.GetAppointmentCountForDateAsync(date, cancellationToken);
        var sequence = countToday + 1;
        var appointmentNumber = $"APT-{todayStr}-{sequence:D4}";

        var appointment = new Appointment
        {
            AppointmentNumber = appointmentNumber,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ClinicId = request.ClinicId,
            AppointmentStatusId = status.AppointmentStatusId,
            ScheduledDateTime = request.ScheduledDateTime,
            ReasonForVisit = request.ReasonForVisit,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _appointmentRepository.CreateAsync(appointment, cancellationToken);
        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        // Fetch details (with eagerly loaded entities) to return DTO
        var detail = await _appointmentRepository.GetDetailByIdAsync(created.AppointmentId, cancellationToken);

        try
        {
            await _notificationService.SendNotificationAsync(
                "APPOINTMENT_BOOKED",
                patient.UserId,
                created.AppointmentId,
                new Dictionary<string, string>
                {
                    { "PatientName", $"{patient.FirstName} {patient.LastName}" },
                    { "DoctorName", $"{doctor.FirstName} {doctor.LastName}" },
                    { "AppointmentNumber", created.AppointmentNumber },
                    { "ScheduledTime", created.ScheduledDateTime.ToString("g") }
                },
                cancellationToken
            );
        }
        catch
        {
            // Fail-safe: do not crash the booking if notifications fail
        }

        return MapToDetailDto(detail ?? created);
    }

    public async Task<AppointmentDetailDto> UpdateAppointmentStatusAsync(
        int appointmentId, 
        string statusName, 
        string? notes, 
        int? userId, 
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateStatusValidator, new UpdateAppointmentStatusRequestDto { StatusName = statusName, Notes = notes }, cancellationToken);

        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        var currentStatus = await _appointmentRepository.GetStatusByIdAsync(appointment.AppointmentStatusId, cancellationToken)
            ?? throw new NotFoundException("Current status not found.");

        if (currentStatus.StatusName == "Completed" || currentStatus.StatusName == "Cancelled")
        {
            throw new ConflictException($"Cannot modify a {currentStatus.StatusName} appointment.");
        }

        var targetStatus = await _appointmentRepository.GetStatusByNameAsync(statusName, cancellationToken)
            ?? throw new NotFoundException($"Target status '{statusName}' not found.");

        // Check permissions: Patient vs Staff
        if (userId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken)
                ?? throw new NotFoundException("User not found.");

            var isStaff = user.UserRoles.Any(ur => AppRoles.Staff.Contains(ur.Role.RoleName));

            if (!isStaff)
            {
                // Must be patient. Patients can only self-cancel their own Pending/Confirmed appointments.
                var patient = await _patientRepository.GetByUserIdAsync(userId.Value, cancellationToken);
                if (patient == null || appointment.PatientId != patient.PatientId)
                {
                    throw new UnauthorizedException("You are not authorized to update this appointment.");
                }

                if (statusName != "Cancelled")
                {
                    throw new ConflictException("Patients are only allowed to cancel their appointments.");
                }

                if (currentStatus.StatusName != "Pending" && currentStatus.StatusName != "Confirmed")
                {
                    throw new ConflictException("Appointments can only be cancelled if they are Pending or Confirmed.");
                }
            }
            else
            {
                // Staff can do any valid transition.
                // InProgress is locked from patient, but staff can complete or cancel it.
                // If it is InProgress, staff can move to Completed or Cancelled.
                if (currentStatus.StatusName == "InProgress" && statusName != "Completed" && statusName != "Cancelled")
                {
                    throw new ConflictException("InProgress appointments can only be transitioned to Completed or Cancelled.");
                }
            }
        }

        appointment.AppointmentStatusId = targetStatus.AppointmentStatusId;
        appointment.Notes = notes;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        var detail = await _appointmentRepository.GetDetailByIdAsync(appointment.AppointmentId, cancellationToken);

        if (statusName == "Confirmed" || statusName == "Cancelled")
        {
            try
            {
                var pat = await _patientRepository.GetByIdAsync(appointment.PatientId, cancellationToken);
                var doc = await _doctorRepository.GetByIdAsync(appointment.DoctorId, cancellationToken);
                if (pat != null && doc != null)
                {
                    var templateCode = statusName == "Confirmed" ? "APPOINTMENT_CONFIRMED" : "APPOINTMENT_CANCELLED";
                    await _notificationService.SendNotificationAsync(
                        templateCode,
                        pat.UserId,
                        appointment.AppointmentId,
                        new Dictionary<string, string>
                        {
                            { "PatientName", $"{pat.FirstName} {pat.LastName}" },
                            { "DoctorName", $"{doc.FirstName} {doc.LastName}" },
                            { "AppointmentNumber", appointment.AppointmentNumber },
                            { "ScheduledTime", appointment.ScheduledDateTime.ToString("g") },
                            { "Reason", notes ?? "No reason provided" }
                        },
                        cancellationToken
                    );
                }
            }
            catch
            {
                // Fail-safe
            }
        }

        return MapToDetailDto(detail ?? appointment);
    }

    public async Task<AppointmentDetailDto> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetDetailByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        return MapToDetailDto(appointment);
    }

    public async Task<PagedResult<AppointmentListItemDto>> GetPagedAsync(
        string? search, 
        int? statusId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _appointmentRepository.GetPagedAsync(search, statusId, pageNumber, pageSize, cancellationToken);
        return new PagedResult<AppointmentListItemDto>
        {
            Items = items.Select(MapToListItemDto),
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<AppointmentListItemDto>> GetActiveAppointmentsByPatientIdAsync(
        int patientId, 
        CancellationToken cancellationToken = default)
    {
        var items = await _appointmentRepository.GetActiveAppointmentsByPatientIdAsync(patientId, cancellationToken);
        return items.Select(MapToListItemDto).ToList();
    }

    public async Task<IReadOnlyList<AppointmentListItemDto>> GetAppointmentsByDoctorIdAsync(
        int doctorId, 
        CancellationToken cancellationToken = default)
    {
        var items = await _appointmentRepository.GetAppointmentsByDoctorIdAsync(doctorId, cancellationToken);
        return items.Select(MapToListItemDto).ToList();
    }

    public async Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(
        int doctorId, 
        DateOnly date, 
        CancellationToken cancellationToken = default)
    {
        var schedules = await _doctorRepository.GetSchedulesByDoctorIdAsync(doctorId, cancellationToken);
        var daySchedule = schedules.FirstOrDefault(s => s.DayOfWeek == (int)date.DayOfWeek && s.IsActive);
        
        if (daySchedule == null)
        {
            return Array.Empty<AvailableSlotDto>();
        }

        var slots = new List<AvailableSlotDto>();
        var currentStart = daySchedule.StartTime;
        var slotDuration = TimeSpan.FromMinutes(daySchedule.SlotDurationMinutes);

        while (currentStart + slotDuration <= daySchedule.EndTime)
        {
            var currentEnd = currentStart + slotDuration;
            slots.Add(new AvailableSlotDto
            {
                StartTime = currentStart.ToString(@"hh\:mm"),
                EndTime = currentEnd.ToString(@"hh\:mm"),
                IsAvailable = true
            });
            currentStart = currentEnd;
        }

        // Exclude slots that overlap with existing appointments that are not Cancelled
        var appointments = await _appointmentRepository.GetAppointmentsByDoctorAndDateAsync(doctorId, date, cancellationToken);
        var appTimes = appointments.Select(a =>
        {
            var appStart = a.ScheduledDateTime.TimeOfDay;
            var appEnd = appStart + slotDuration;
            return (Start: appStart, End: appEnd);
        }).ToList();

        // Enforce scheduling dates must be in the future
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isToday = date == today;
        var nowTimeOfDay = DateTime.UtcNow.TimeOfDay;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var slotStart = TimeSpan.Parse(slot.StartTime);
            var slotEnd = TimeSpan.Parse(slot.EndTime);

            // Overlap check
            var overlaps = appTimes.Any(app => app.Start < slotEnd && slotStart < app.End);
            var isPast = isToday && slotStart <= nowTimeOfDay;

            if (overlaps || isPast)
            {
                slots[i] = slot with { IsAvailable = false };
            }
        }

        return slots;
    }

    public async Task<IReadOnlyList<AppointmentStatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _appointmentRepository.GetStatusesAsync(cancellationToken);
        return statuses.Select(s => new AppointmentStatusDto
        {
            AppointmentStatusId = s.AppointmentStatusId,
            StatusName = s.StatusName,
            Description = s.Description
        }).ToList();
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new AppValidationException("Validation failed.", result.Errors.Select(e => e.ErrorMessage));
        }
    }

    private static AppointmentDetailDto MapToDetailDto(Appointment a) => new()
    {
        AppointmentId = a.AppointmentId,
        AppointmentNumber = a.AppointmentNumber,
        PatientId = a.PatientId,
        PatientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : "Unknown",
        DoctorId = a.DoctorId,
        DoctorName = a.Doctor != null ? $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}" : "Unknown",
        SpecializationName = a.Doctor?.Specialization?.SpecializationName ?? "Unknown",
        ClinicId = a.ClinicId,
        ClinicName = a.Clinic?.ClinicName ?? "Unknown",
        AppointmentStatusId = a.AppointmentStatusId,
        StatusName = a.AppointmentStatus?.StatusName ?? "Unknown",
        ScheduledDateTime = a.ScheduledDateTime,
        ReasonForVisit = a.ReasonForVisit,
        Notes = a.Notes,
        CreatedAt = a.CreatedAt,
        CurrentPriorityClassificationId = a.CurrentPriorityClassificationId,
        CurrentPriorityLevelName = a.CurrentPriorityClassification?.PredictedPriorityLevel?.LevelName,
        CurrentPriorityColorHex = a.CurrentPriorityClassification?.PredictedPriorityLevel?.ColorHex
    };

    private static AppointmentListItemDto MapToListItemDto(Appointment a) => new()
    {
        AppointmentId = a.AppointmentId,
        AppointmentNumber = a.AppointmentNumber,
        PatientId = a.PatientId,
        PatientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : "Unknown",
        DoctorName = a.Doctor != null ? $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}" : "Unknown",
        SpecializationName = a.Doctor?.Specialization?.SpecializationName ?? "Unknown",
        ClinicName = a.Clinic?.ClinicName ?? "Unknown",
        StatusName = a.AppointmentStatus?.StatusName ?? "Unknown",
        ScheduledDateTime = a.ScheduledDateTime,
        CurrentPriorityLevelName = a.CurrentPriorityClassification?.PredictedPriorityLevel?.LevelName,
        CurrentPriorityColorHex = a.CurrentPriorityClassification?.PredictedPriorityLevel?.ColorHex
    };
}
