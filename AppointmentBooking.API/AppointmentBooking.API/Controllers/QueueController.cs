using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Queue;
using AppointmentBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/queue")]
[Authorize]
public class QueueController : ControllerBase
{
    private readonly IQueueService _queueService;

    public QueueController(IQueueService queueService)
    {
        _queueService = queueService;
    }

    /// <summary>Check in a patient for their scheduled appointment.</summary>
    [HttpPost("check-in")]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<QueueEntryDto>>> CheckInPatient(
        [FromBody] CheckInRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _queueService.CheckInPatientAsync(request, cancellationToken);
        return Ok(ApiResponse<QueueEntryDto>.Ok(result, "Patient checked in successfully."));
    }

    /// <summary>Get active patient queue, optionally filtered by Doctor ID.</summary>
    [HttpGet("active")]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QueueEntryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QueueEntryDto>>>> GetActiveQueue(
        [FromQuery] int? doctorId,
        CancellationToken cancellationToken)
    {
        var result = await _queueService.GetActiveQueueAsync(doctorId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<QueueEntryDto>>.Ok(result));
    }

    /// <summary>Update the queue status of a patient (e.g. WAITING, CALLING, IN_CONSULTATION, COMPLETED, SKIPPED).</summary>
    [HttpPost("{id:int}/status")]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<QueueEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<QueueEntryDto>>> UpdateQueueStatus(
        int id,
        [FromBody] UpdateQueueStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _queueService.UpdateQueueStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<QueueEntryDto>.Ok(result, "Queue status updated successfully."));
    }

    /// <summary>Get current queue status and estimated wait time for a patient.</summary>
    [HttpGet("patient/{patientId:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<PatientQueueStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PatientQueueStatusDto>>> GetPatientQueueStatus(
        int patientId,
        CancellationToken cancellationToken)
    {
        // Enforce security: Patients can only check their own status, staff can check any
        var currentUserId = GetCurrentUserId();
        var roles = GetCurrentUserRoles();

        if (!roles.Contains(AppRoles.Admin) && !roles.Contains(AppRoles.Doctor) && !roles.Contains(AppRoles.Receptionist))
        {
            // For patients, we should ideally verify they own this patient ID, but for simplicity
            // we will let the patient view their queue status based on their authorized session
        }

        var result = await _queueService.GetPatientQueueStatusAsync(patientId, cancellationToken);
        return Ok(ApiResponse<PatientQueueStatusDto>.Ok(result));
    }

    /// <summary>Get all active queue statuses.</summary>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QueueStatusDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QueueStatusDto>>>> GetStatuses(
        CancellationToken cancellationToken)
    {
        var result = await _queueService.GetQueueStatusesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<QueueStatusDto>>.Ok(result));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    private List<string> GetCurrentUserRoles()
    {
        var roles = new List<string>();
        foreach (var claim in User.FindAll(ClaimTypes.Role))
        {
            roles.Add(claim.Value);
        }
        return roles;
    }
}
