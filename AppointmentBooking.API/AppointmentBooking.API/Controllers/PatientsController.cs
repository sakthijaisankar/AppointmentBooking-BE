using System.Security.Claims;
using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Patients;
using AppointmentBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>Create patient clinical profile (Patient role, linked to authenticated UserId).</summary>
    [HttpPost("profile")]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PatientDetailDto>>> CreateProfile(
        [FromBody] CreatePatientProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _patientService.CreateProfileAsync(GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiResponse<PatientDetailDto>.Ok(result, "Patient profile created successfully."));
    }

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PatientDetailDto>>> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _patientService.GetMyProfileAsync(GetRequiredUserId(), cancellationToken);
        if (result is null)
            return Ok(ApiResponse<PatientDetailDto>.Ok(null!, "No patient profile found."));
        return Ok(ApiResponse<PatientDetailDto>.Ok(result));
    }

    [HttpPut("me")]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PatientDetailDto>>> UpdateMyProfile(
        [FromBody] UpdatePatientProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _patientService.UpdateMyProfileAsync(GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiResponse<PatientDetailDto>.Ok(result, "Patient profile updated successfully."));
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientListItemDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _patientService.GetAllAsync(search, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<PatientListItemDto>>.Ok(result));
    }

    [HttpGet("{patientId:int}")]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PatientDetailDto>>> GetById(
        int patientId,
        CancellationToken cancellationToken)
    {
        var result = await _patientService.GetByIdAsync(patientId, GetRequiredUserId(), GetRoles(), cancellationToken);
        return Ok(ApiResponse<PatientDetailDto>.Ok(result));
    }

    // Emergency Contacts
    [HttpPost("me/emergency-contacts")]
    [Authorize(Roles = AppRoles.Patient)]
    public async Task<ActionResult<ApiResponse<EmergencyContactDto>>> AddMyEmergencyContact(
        [FromBody] CreateEmergencyContactRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _patientService.AddEmergencyContactAsync(GetRequiredUserId(), GetRoles(), null, request, cancellationToken);
        return Ok(ApiResponse<EmergencyContactDto>.Ok(result, "Emergency contact added."));
    }

    [HttpGet("me/emergency-contacts")]
    [Authorize(Roles = AppRoles.Patient)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmergencyContactDto>>>> GetMyEmergencyContacts(
        CancellationToken cancellationToken)
    {
        var result = await _patientService.GetEmergencyContactsAsync(GetRequiredUserId(), GetRoles(), null, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<EmergencyContactDto>>.Ok(result));
    }

    [HttpPut("emergency-contacts/{contactId:int}")]
    public async Task<ActionResult<ApiResponse<EmergencyContactDto>>> UpdateEmergencyContact(
        int contactId,
        [FromBody] UpdateEmergencyContactRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _patientService.UpdateEmergencyContactAsync(GetRequiredUserId(), GetRoles(), contactId, request, cancellationToken);
        return Ok(ApiResponse<EmergencyContactDto>.Ok(result, "Emergency contact updated."));
    }

    [HttpDelete("emergency-contacts/{contactId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteEmergencyContact(
        int contactId,
        CancellationToken cancellationToken)
    {
        await _patientService.DeleteEmergencyContactAsync(GetRequiredUserId(), GetRoles(), contactId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Emergency contact deleted."));
    }

    // Medical History
    [HttpPost("me/medical-history")]
    [Authorize(Roles = AppRoles.Patient)]
    public async Task<ActionResult<ApiResponse<PatientMedicalHistoryDto>>> AddMyMedicalHistory(
        [FromBody] CreateMedicalHistoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _patientService.AddMedicalHistoryAsync(GetRequiredUserId(), GetRoles(), null, request, cancellationToken);
        return Ok(ApiResponse<PatientMedicalHistoryDto>.Ok(result, "Medical history added."));
    }

    [HttpGet("me/medical-history")]
    [Authorize(Roles = AppRoles.Patient)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PatientMedicalHistoryDto>>>> GetMyMedicalHistory(
        CancellationToken cancellationToken)
    {
        var result = await _patientService.GetMedicalHistoryAsync(GetRequiredUserId(), GetRoles(), null, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PatientMedicalHistoryDto>>.Ok(result));
    }

    [HttpPut("medical-history/{historyId:int}")]
    public async Task<ActionResult<ApiResponse<PatientMedicalHistoryDto>>> UpdateMedicalHistory(
        int historyId,
        [FromBody] UpdateMedicalHistoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _patientService.UpdateMedicalHistoryAsync(GetRequiredUserId(), GetRoles(), historyId, request, cancellationToken);
        return Ok(ApiResponse<PatientMedicalHistoryDto>.Ok(result, "Medical history updated."));
    }

    [HttpDelete("medical-history/{historyId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMedicalHistory(
        int historyId,
        CancellationToken cancellationToken)
    {
        await _patientService.DeleteMedicalHistoryAsync(GetRequiredUserId(), GetRoles(), historyId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Medical history deleted."));
    }

    // Documents
    [HttpPost("me/documents")]
    [Authorize(Roles = AppRoles.Patient)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<PatientDocumentDto>>> UploadMyDocument(
        [FromForm] UploadDocumentRequestDto metadata,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("File is required."));

        await using var stream = file.OpenReadStream();
        var result = await _patientService.UploadDocumentAsync(
            GetRequiredUserId(), GetRoles(), null, metadata, stream,
            file.FileName, file.ContentType, file.Length, cancellationToken);
        return Ok(ApiResponse<PatientDocumentDto>.Ok(result, "Document uploaded."));
    }

    [HttpGet("me/documents")]
    [Authorize(Roles = AppRoles.Patient)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PatientDocumentDto>>>> GetMyDocuments(
        CancellationToken cancellationToken)
    {
        var result = await _patientService.GetDocumentsAsync(GetRequiredUserId(), GetRoles(), null, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PatientDocumentDto>>.Ok(result));
    }

    [HttpGet("documents/{documentId:int}/download")]
    public async Task<IActionResult> DownloadDocument(int documentId, CancellationToken cancellationToken)
    {
        var (stream, contentType, fileName) = await _patientService.DownloadDocumentAsync(
            GetRequiredUserId(), GetRoles(), documentId, cancellationToken);
        return File(stream, contentType, fileName);
    }

    [HttpDelete("documents/{documentId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDocument(
        int documentId,
        CancellationToken cancellationToken)
    {
        await _patientService.DeleteDocumentAsync(GetRequiredUserId(), GetRoles(), documentId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null!, "Document deleted."));
    }

    private int GetRequiredUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private IReadOnlyList<string> GetRoles() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
}
