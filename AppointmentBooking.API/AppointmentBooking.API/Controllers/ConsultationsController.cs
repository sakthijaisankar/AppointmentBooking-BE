using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Consultation;
using AppointmentBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/consultations")]
[Authorize]
public class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultationService;

    public ConsultationsController(IConsultationService consultationService)
    {
        _consultationService = consultationService;
    }

    /// <summary>Create a new consultation for an appointment (Doctor/Admin only).</summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.DoctorOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> CreateConsultation(
        [FromBody] CreateConsultationRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await _consultationService.CreateConsultationAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.ConsultationId },
            ApiResponse<ConsultationDto>.Ok(result, "Consultation created successfully."));
    }

    /// <summary>Get consultation by ID (Staff).</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _consultationService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ConsultationDto>.Ok(result));
    }

    /// <summary>Get consultation by Appointment ID (Staff or own Patient).</summary>
    [HttpGet("appointment/{appointmentId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> GetByAppointmentId(
        int appointmentId,
        CancellationToken cancellationToken)
    {
        var result = await _consultationService.GetByAppointmentIdAsync(appointmentId, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<ConsultationDto>.Fail("No consultation found for this appointment."));

        return Ok(ApiResponse<ConsultationDto>.Ok(result));
    }

    /// <summary>Update consultation diagnosis and notes (Doctor/Admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthPolicies.DoctorOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> UpdateConsultation(
        int id,
        [FromBody] UpdateConsultationRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await _consultationService.UpdateConsultationAsync(id, request, userId, cancellationToken);
        return Ok(ApiResponse<ConsultationDto>.Ok(result, "Consultation updated successfully."));
    }

    /// <summary>Add a prescription to a consultation (Doctor/Admin).</summary>
    [HttpPost("{id:int}/prescriptions")]
    [Authorize(Policy = AuthPolicies.DoctorOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> AddPrescription(
        int id,
        [FromBody] AddPrescriptionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _consultationService.AddPrescriptionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ConsultationDto>.Ok(result, "Prescription added successfully."));
    }

    /// <summary>Remove a prescription from a consultation (Doctor/Admin).</summary>
    [HttpDelete("{id:int}/prescriptions/{prescriptionId:int}")]
    [Authorize(Policy = AuthPolicies.DoctorOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConsultationDto>>> DeletePrescription(
        int id,
        int prescriptionId,
        CancellationToken cancellationToken)
    {
        var result = await _consultationService.DeletePrescriptionAsync(id, prescriptionId, cancellationToken);
        return Ok(ApiResponse<ConsultationDto>.Ok(result, "Prescription removed successfully."));
    }

    /// <summary>Get full consultation history for a patient (Staff or own Patient).</summary>
    [HttpGet("patient/{patientId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ConsultationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ConsultationSummaryDto>>>> GetPatientHistory(
        int patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _consultationService.GetPatientHistoryAsync(patientId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ConsultationSummaryDto>>.Ok(result));
    }

    /// <summary>Get all consultations by a doctor (Doctor/Admin).</summary>
    [HttpGet("doctor/{doctorId:int}")]
    [Authorize(Policy = AuthPolicies.DoctorOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ConsultationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ConsultationSummaryDto>>>> GetDoctorConsultations(
        int doctorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _consultationService.GetDoctorConsultationsAsync(doctorId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ConsultationSummaryDto>>.Ok(result));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
