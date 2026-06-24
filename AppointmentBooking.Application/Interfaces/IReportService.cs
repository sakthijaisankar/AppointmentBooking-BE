using AppointmentBooking.Application.DTOs.Report;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Application.Interfaces;

public interface IReportService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<AppointmentStatsDto> GetAppointmentStatsAsync(CancellationToken cancellationToken = default);
    Task<QueueAndEmergencyStatsDto> GetQueueAndEmergencyStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorPerformanceDto>> GetDoctorPerformanceStatsAsync(CancellationToken cancellationToken = default);
    Task<PatientAnalyticsDto> GetPatientAnalyticsAsync(CancellationToken cancellationToken = default);
}
