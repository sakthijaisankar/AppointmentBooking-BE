using AppointmentBooking.Application.DTOs.Report;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Repository.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Application.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var model = await _reportRepository.GetDashboardSummaryAsync(cancellationToken);
        return new DashboardSummaryDto(
            TotalPatients: model.TotalPatients,
            AppointmentsToday: model.AppointmentsToday,
            ActiveQueueCount: model.ActiveQueueCount,
            AvgWaitTimeMinutes: model.AvgWaitTimeMinutes
        );
    }

    public async Task<AppointmentStatsDto> GetAppointmentStatsAsync(CancellationToken cancellationToken = default)
    {
        var (statuses, trends) = await _reportRepository.GetAppointmentStatsAsync(cancellationToken);
        return new AppointmentStatsDto(
            Statuses: statuses.Select(s => new StatusDistributionDto(s.StatusName, s.Count)).ToList(),
            Trends: trends.Select(t => new MonthlyTrendDto(t.Year, t.Month, t.Volume)).ToList()
        );
    }

    public async Task<QueueAndEmergencyStatsDto> GetQueueAndEmergencyStatsAsync(CancellationToken cancellationToken = default)
    {
        var (triage, overrideCount) = await _reportRepository.GetQueueAndEmergencyStatsAsync(cancellationToken);
        return new QueueAndEmergencyStatsDto(
            Triage: triage.Select(t => new TriageDistributionDto(t.LevelName, t.Count)).ToList(),
            OverrideCount: overrideCount
        );
    }

    public async Task<IReadOnlyList<DoctorPerformanceDto>> GetDoctorPerformanceStatsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _reportRepository.GetDoctorPerformanceStatsAsync(cancellationToken);
        return list.Select(d => new DoctorPerformanceDto(
            DoctorId: d.DoctorId,
            DoctorName: d.DoctorName,
            SpecializationName: d.SpecializationName,
            TotalConsultations: d.TotalConsultations,
            AvgWaitTimeMinutes: d.AvgWaitTimeMinutes
        )).ToList();
    }

    public async Task<PatientAnalyticsDto> GetPatientAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var (ageGroups, genders) = await _reportRepository.GetPatientAnalyticsAsync(cancellationToken);
        return new PatientAnalyticsDto(
            AgeGroups: ageGroups.Select(a => new AgeGroupDto(a.AgeGroup, a.Count)).ToList(),
            Genders: genders.Select(g => new GenderDistributionDto(g.Gender, g.Count)).ToList()
        );
    }
}
