using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Doctors;
using AppointmentBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    /// <summary>Get a paginated list of doctors, optionally filtered by specialization or search term.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DoctorProfileDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DoctorProfileDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? specializationId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _doctorService.GetAllAsync(search, specializationId, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<DoctorProfileDto>>.Ok(result));
    }

    /// <summary>Get detailed info about a doctor by ID (including clinic and active schedules).</summary>
    [HttpGet("{doctorId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DoctorDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DoctorDetailDto>>> GetById(
        int doctorId,
        CancellationToken cancellationToken)
    {
        var result = await _doctorService.GetByIdAsync(doctorId, cancellationToken);
        return Ok(ApiResponse<DoctorDetailDto>.Ok(result));
    }

    /// <summary>Get the active doctor profile linked to the logged-in user.</summary>
    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Doctor)]
    [ProducesResponseType(typeof(ApiResponse<DoctorDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DoctorDetailDto>>> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var result = await _doctorService.GetByUserIdAsync(userId, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<DoctorDetailDto>.Fail("Doctor profile not found for the logged-in user."));

        return Ok(ApiResponse<DoctorDetailDto>.Ok(result));
    }

    /// <summary>Create a new Doctor Profile. Linked to an existing User who has the Doctor role.</summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<DoctorDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DoctorDetailDto>>> CreateProfile(
        [FromBody] CreateDoctorProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _doctorService.CreateProfileAsync(request, cancellationToken);
        return Ok(ApiResponse<DoctorDetailDto>.Ok(result, "Doctor profile created successfully."));
    }

    /// <summary>Update doctor profile details.</summary>
    [HttpPut("{doctorId:int}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DoctorDetailDto>>> UpdateProfile(
        int doctorId,
        [FromBody] UpdateDoctorProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureDoctorOrAdminAccessAsync(doctorId);
        var result = await _doctorService.UpdateProfileAsync(doctorId, request, cancellationToken);
        return Ok(ApiResponse<DoctorDetailDto>.Ok(result, "Doctor profile updated successfully."));
    }

    /// <summary>Soft delete a doctor profile.</summary>
    [HttpDelete("{doctorId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProfile(
        int doctorId,
        CancellationToken cancellationToken)
    {
        await _doctorService.DeleteProfileAsync(doctorId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Doctor profile soft-deleted successfully."));
    }

    // Schedules Management

    /// <summary>Get schedules for a doctor.</summary>
    [HttpGet("{doctorId:int}/schedules")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DoctorScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DoctorScheduleDto>>>> GetSchedules(
        int doctorId,
        CancellationToken cancellationToken)
    {
        var result = await _doctorService.GetSchedulesAsync(doctorId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DoctorScheduleDto>>.Ok(result));
    }

    /// <summary>Add a scheduling block for a doctor.</summary>
    [HttpPost("{doctorId:int}/schedules")]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DoctorScheduleDto>>> AddSchedule(
        int doctorId,
        [FromBody] CreateDoctorScheduleRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureDoctorOrAdminAccessAsync(doctorId);
        var result = await _doctorService.AddScheduleAsync(doctorId, request, cancellationToken);
        return Ok(ApiResponse<DoctorScheduleDto>.Ok(result, "Schedule added successfully."));
    }

    /// <summary>Update an existing schedule entry.</summary>
    [HttpPut("schedules/{scheduleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DoctorScheduleDto>>> UpdateSchedule(
        int scheduleId,
        [FromBody] UpdateDoctorScheduleRequestDto request,
        CancellationToken cancellationToken)
    {
        var scheduleDocId = await GetDoctorIdByScheduleIdAsync(scheduleId, cancellationToken);
        await EnsureDoctorOrAdminAccessAsync(scheduleDocId);

        var result = await _doctorService.UpdateScheduleAsync(scheduleId, request, cancellationToken);
        return Ok(ApiResponse<DoctorScheduleDto>.Ok(result, "Schedule updated successfully."));
    }

    /// <summary>Soft delete an existing schedule entry.</summary>
    [HttpDelete("schedules/{scheduleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSchedule(
        int scheduleId,
        CancellationToken cancellationToken)
    {
        var scheduleDocId = await GetDoctorIdByScheduleIdAsync(scheduleId, cancellationToken);
        await EnsureDoctorOrAdminAccessAsync(scheduleDocId);

        await _doctorService.DeleteScheduleAsync(scheduleId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Schedule deleted successfully."));
    }

    // Specializations Management

    /// <summary>Retrieve active specializations master list.</summary>
    [HttpGet("specializations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SpecializationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SpecializationDto>>>> GetSpecializations(
        CancellationToken cancellationToken)
    {
        var result = await _doctorService.GetSpecializationsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SpecializationDto>>.Ok(result));
    }

    /// <summary>Create a specialization.</summary>
    [HttpPost("specializations")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<SpecializationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SpecializationDto>>> CreateSpecialization(
        [FromBody] CreateSpecializationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _doctorService.CreateSpecializationAsync(request, cancellationToken);
        return Ok(ApiResponse<SpecializationDto>.Ok(result, "Specialization created successfully."));
    }

    /// <summary>Update an existing specialization.</summary>
    [HttpPut("specializations/{specializationId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<SpecializationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SpecializationDto>>> UpdateSpecialization(
        int specializationId,
        [FromBody] UpdateSpecializationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _doctorService.UpdateSpecializationAsync(specializationId, request, cancellationToken);
        return Ok(ApiResponse<SpecializationDto>.Ok(result, "Specialization updated successfully."));
    }

    /// <summary>Soft delete a specialization.</summary>
    [HttpDelete("specializations/{specializationId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSpecialization(
        int specializationId,
        CancellationToken cancellationToken)
    {
        await _doctorService.DeleteSpecializationAsync(specializationId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Specialization soft-deleted successfully."));
    }

    // Helper methods for access checking and ID resolving
    private int GetRequiredUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private async Task EnsureDoctorOrAdminAccessAsync(int doctorId)
    {
        if (User.IsInRole(AppRoles.Admin)) return;

        if (User.IsInRole(AppRoles.Doctor))
        {
            var userId = GetRequiredUserId();
            var doc = await _doctorService.GetByUserIdAsync(userId);
            if (doc != null && doc.DoctorId == doctorId) return;
        }

        throw new UnauthorizedAccessException("Access denied. You can only manage your own profile and schedules.");
    }

    private async Task<int> GetDoctorIdByScheduleIdAsync(int scheduleId, CancellationToken cancellationToken)
    {
        return await _doctorService.GetDoctorIdByScheduleIdAsync(scheduleId, cancellationToken);
    }
}
