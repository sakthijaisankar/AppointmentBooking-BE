using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Report;
using AppointmentBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthPolicies.StaffOnly)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Get overall KPI stats for summary dashboard tiles.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDashboardSummaryAsync(cancellationToken);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(result));
    }

    /// <summary>Get appointment statistics (by status and monthly trend volume).</summary>
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AppointmentStatsDto>>> GetAppointments(CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetAppointmentStatsAsync(cancellationToken);
        return Ok(ApiResponse<AppointmentStatsDto>.Ok(result));
    }

    /// <summary>Get queue and emergency priority metrics.</summary>
    [HttpGet("queue-emergency")]
    [ProducesResponseType(typeof(ApiResponse<QueueAndEmergencyStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<QueueAndEmergencyStatsDto>>> GetQueueAndEmergency(CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetQueueAndEmergencyStatsAsync(cancellationToken);
        return Ok(ApiResponse<QueueAndEmergencyStatsDto>.Ok(result));
    }

    /// <summary>Get doctor consultations volume and average wait times.</summary>
    [HttpGet("doctors")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DoctorPerformanceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DoctorPerformanceDto>>>> GetDoctors(CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDoctorPerformanceStatsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DoctorPerformanceDto>>.Ok(result));
    }

    /// <summary>Get patient demographics and age distribution analytics.</summary>
    [HttpGet("patients")]
    [ProducesResponseType(typeof(ApiResponse<PatientAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PatientAnalyticsDto>>> GetPatients(CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetPatientAnalyticsAsync(cancellationToken);
        return Ok(ApiResponse<PatientAnalyticsDto>.Ok(result));
    }
}
