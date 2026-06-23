using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.Symptom;
using AppointmentBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/symptoms")]
[Authorize]
public class SymptomsController : ControllerBase
{
    private readonly ISymptomService _symptomService;

    public SymptomsController(ISymptomService symptomService)
    {
        _symptomService = symptomService;
    }

    /// <summary>Get list of all active symptoms for patient checklist selection.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SymptomDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SymptomDto>>>> GetActiveSymptoms(CancellationToken cancellationToken)
    {
        var result = await _symptomService.GetActiveSymptomsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SymptomDto>>.Ok(result));
    }

    /// <summary>Submit symptoms checklist and severity scores for a scheduled appointment.</summary>
    [HttpPost("appointment/{appointmentId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> SubmitSymptoms(
        int appointmentId,
        [FromBody] SubmitSymptomsRequestDto request,
        CancellationToken cancellationToken)
    {
        if (appointmentId != request.AppointmentId)
        {
            return BadRequest(ApiResponse<object>.Fail("Appointment ID mismatch."));
        }

        var userId = GetRequiredUserId();
        await _symptomService.SubmitSymptomsAsync(request, userId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Symptoms submitted successfully."));
    }

    /// <summary>Get submitted symptoms detail card for an appointment.</summary>
    [HttpGet("appointment/{appointmentId:int}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentSymptomsDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AppointmentSymptomsDetailDto>>> GetSymptomsByAppointmentId(
        int appointmentId,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var result = await _symptomService.GetSymptomsByAppointmentIdAsync(appointmentId, userId, cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<AppointmentSymptomsDetailDto>.Fail("No symptoms submitted for this appointment."));
        }

        return Ok(ApiResponse<AppointmentSymptomsDetailDto>.Ok(result));
    }

    private int GetRequiredUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
