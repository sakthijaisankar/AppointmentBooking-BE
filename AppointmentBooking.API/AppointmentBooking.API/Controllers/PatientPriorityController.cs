using System.Security.Claims;
using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.PatientPriority;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/patient-priority")]
[Authorize]
public class PatientPriorityController : ControllerBase
{
    private readonly IPatientPriorityService _patientPriorityService;

    public PatientPriorityController(IPatientPriorityService patientPriorityService)
    {
        _patientPriorityService = patientPriorityService;
    }

    /// <summary>Classify patient priority using ML engine based on clinical features.</summary>
    [HttpPost("patients/{patientId:int}/classify")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor},{AppRoles.Receptionist}")]
    [ProducesResponseType(typeof(ApiResponse<PatientPriorityClassificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PatientPriorityClassificationDto>>> ClassifyPatient(
        int patientId,
        [FromBody] ClassifyPatientPriorityRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _patientPriorityService.ClassifyPatientAsync(patientId, request, userId, cancellationToken);
        return Ok(ApiResponse<PatientPriorityClassificationDto>.Ok(result, "Patient priority classified successfully."));
    }

    /// <summary>Get current active priority classification for a patient.</summary>
    [HttpGet("patients/{patientId:int}/current")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor},{AppRoles.Receptionist}")]
    [ProducesResponseType(typeof(ApiResponse<PatientPriorityClassificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PatientPriorityClassificationDto>>> GetCurrentClassification(
        int patientId,
        CancellationToken cancellationToken)
    {
        var result = await _patientPriorityService.GetCurrentClassificationAsync(patientId, cancellationToken);
        if (result is null)
            return Ok(ApiResponse<PatientPriorityClassificationDto>.Ok(null!, "No classification found for this patient."));

        return Ok(ApiResponse<PatientPriorityClassificationDto>.Ok(result));
    }

    /// <summary>Get paginated classification history for a patient.</summary>
    [HttpGet("patients/{patientId:int}/history")]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientPriorityClassificationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientPriorityClassificationDto>>>> GetClassificationHistory(
        int patientId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _patientPriorityService.GetClassificationHistoryAsync(patientId, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<PatientPriorityClassificationDto>>.Ok(result));
    }

    /// <summary>Override ML classification with clinical staff decision.</summary>
    [HttpPost("classifications/{classificationId:int}/override")]
    [Authorize(Policy = AuthPolicies.DoctorOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PatientPriorityClassificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PatientPriorityClassificationDto>>> OverrideClassification(
        int classificationId,
        [FromBody] OverridePatientPriorityRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User ID not found in token.");
        var result = await _patientPriorityService.OverrideClassificationAsync(classificationId, request, userId, cancellationToken);
        return Ok(ApiResponse<PatientPriorityClassificationDto>.Ok(result, "Classification overridden successfully."));
    }

    /// <summary>Get all active priority levels.</summary>
    [HttpGet("levels")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PriorityLevelDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PriorityLevelDto>>>> GetPriorityLevels(
        CancellationToken cancellationToken)
    {
        var result = await _patientPriorityService.GetPriorityLevelsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PriorityLevelDto>>.Ok(result));
    }

    /// <summary>Get active ML model version metadata.</summary>
    [HttpGet("model/active")]
    [Authorize(Policy = AuthPolicies.DoctorOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<MlModelVersionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MlModelVersionDto>>> GetActiveModel(
        CancellationToken cancellationToken)
    {
        var result = await _patientPriorityService.GetActiveModelVersionAsync(cancellationToken);
        return Ok(ApiResponse<MlModelVersionDto>.Ok(result!));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }
}
