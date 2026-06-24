using AppointmentBooking.Repository.Models;

namespace AppointmentBooking.Repository.Interfaces;

public interface IReportRepository
{
    Task<DashboardSummaryModel> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StatusDistributionModel> Statuses, IReadOnlyList<MonthlyTrendModel> Trends)> GetAppointmentStatsAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<TriageDistributionModel> Triage, int OverrideCount)> GetQueueAndEmergencyStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorPerformanceModel>> GetDoctorPerformanceStatsAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AgeGroupModel> AgeGroups, IReadOnlyList<GenderDistributionModel> Genders)> GetPatientAnalyticsAsync(CancellationToken cancellationToken = default);
}
