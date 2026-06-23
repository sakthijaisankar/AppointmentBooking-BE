using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Appointment;
using AppointmentBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;

    public AppointmentsController(
        IAppointmentService appointmentService,
        IPatientService patientService,
        IDoctorService doctorService)
    {
        _appointmentService = appointmentService;
        _patientService = patientService;
        _doctorService = doctorService;
    }

    /// <summary>Request/Book a new appointment.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> BookAppointment(
        [FromBody] CreateAppointmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var result = await _appointmentService.BookAppointmentAsync(request, userId, cancellationToken);
        return Ok(ApiResponse<AppointmentDetailDto>.Ok(result, "Appointment booked successfully."));
    }

    /// <summary>Get detailed info about an appointment by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var roles = GetRoles();
        var appointment = await _appointmentService.GetByIdAsync(id, cancellationToken);

        var isStaff = roles.Contains(AppRoles.Admin) || roles.Contains(AppRoles.Receptionist);
        if (!isStaff)
        {
            if (roles.Contains(AppRoles.Doctor))
            {
                var doctor = await _doctorService.GetByUserIdAsync(userId, cancellationToken);
                if (doctor == null || appointment.DoctorId != doctor.DoctorId)
                {
                    return Forbid();
                }
            }
            else if (roles.Contains(AppRoles.Patient))
            {
                var patient = await _patientService.GetMyProfileAsync(userId, cancellationToken);
                if (patient == null || appointment.PatientId != patient.PatientId)
                {
                    return Forbid();
                }
            }
            else
            {
                return Forbid();
            }
        }

        return Ok(ApiResponse<AppointmentDetailDto>.Ok(appointment));
    }

    /// <summary>Update the status of an appointment (e.g. Confirm, Check In, Cancel).</summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> UpdateStatus(
        int id,
        [FromBody] UpdateAppointmentStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var result = await _appointmentService.UpdateAppointmentStatusAsync(id, request.StatusName, request.Notes, userId, cancellationToken);
        return Ok(ApiResponse<AppointmentDetailDto>.Ok(result, "Appointment status updated successfully."));
    }

    /// <summary>Get appointments for the logged-in patient.</summary>
    [HttpGet("patient/me")]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AppointmentListItemDto>>>> GetMyAppointments(CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var patient = await _patientService.GetMyProfileAsync(userId, cancellationToken);
        if (patient == null)
        {
            return Ok(ApiResponse<IReadOnlyList<AppointmentListItemDto>>.Ok(Array.Empty<AppointmentListItemDto>()));
        }

        var result = await _appointmentService.GetActiveAppointmentsByPatientIdAsync(patient.PatientId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AppointmentListItemDto>>.Ok(result));
    }

    /// <summary>Get appointments for the logged-in doctor.</summary>
    [HttpGet("doctor/me")]
    [Authorize(Roles = AppRoles.Doctor)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AppointmentListItemDto>>>> GetDoctorAppointments(CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var doctor = await _doctorService.GetByUserIdAsync(userId, cancellationToken);
        if (doctor == null)
        {
            return NotFound(ApiResponse<IReadOnlyList<AppointmentListItemDto>>.Fail("Doctor profile not found."));
        }

        var result = await _appointmentService.GetAppointmentsByDoctorIdAsync(doctor.DoctorId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AppointmentListItemDto>>.Ok(result));
    }

    /// <summary>Get a paginated overview of all appointments (Staff only).</summary>
    [HttpGet]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AppointmentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AppointmentListItemDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? statusId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _appointmentService.GetPagedAsync(search, statusId, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<AppointmentListItemDto>>.Ok(result));
    }

    /// <summary>Get available doctor schedule time blocks for a date.</summary>
    [HttpGet("available-slots")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AvailableSlotDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AvailableSlotDto>>>> GetAvailableSlots(
        [FromQuery] int doctorId,
        [FromQuery] string date,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return BadRequest(ApiResponse<IReadOnlyList<AvailableSlotDto>>.Fail("Invalid date format. Use YYYY-MM-DD."));
        }

        var result = await _appointmentService.GetAvailableSlotsAsync(doctorId, parsedDate, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AvailableSlotDto>>.Ok(result));
    }

    /// <summary>Get all available appointment statuses.</summary>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentStatusDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AppointmentStatusDto>>>> GetStatuses(CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetStatusesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AppointmentStatusDto>>.Ok(result));
    }

    private int GetRequiredUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private IReadOnlyList<string> GetRoles()
    {
        var roles = new List<string>();
        foreach (var claim in User.FindAll(ClaimTypes.Role))
        {
            roles.Add(claim.Value);
        }
        return roles;
    }
}
