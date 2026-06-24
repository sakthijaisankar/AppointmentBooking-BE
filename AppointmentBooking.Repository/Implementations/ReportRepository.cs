using AppointmentBooking.Database;
using AppointmentBooking.Repository.Interfaces;
using AppointmentBooking.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace AppointmentBooking.Repository.Implementations;

public class ReportRepository : IReportRepository
{
    private readonly AppointmentBookingDbContext _context;

    public ReportRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryModel> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "dbo.sp_GetDashboardSummaryStats";
        command.CommandType = CommandType.StoredProcedure;

        if (command.Connection.State == ConnectionState.Closed)
            await command.Connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new DashboardSummaryModel(
                TotalPatients: reader.GetInt32(0),
                AppointmentsToday: reader.GetInt32(1),
                ActiveQueueCount: reader.GetInt32(2),
                AvgWaitTimeMinutes: reader.GetInt32(3)
            );
        }

        return new DashboardSummaryModel(0, 0, 0, 0);
    }

    public async Task<(IReadOnlyList<StatusDistributionModel> Statuses, IReadOnlyList<MonthlyTrendModel> Trends)> GetAppointmentStatsAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new List<StatusDistributionModel>();
        var trends = new List<MonthlyTrendModel>();

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "dbo.sp_GetAppointmentReport";
        command.CommandType = CommandType.StoredProcedure;

        if (command.Connection.State == ConnectionState.Closed)
            await command.Connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        // 1st Result Set: Status Distribution
        while (await reader.ReadAsync(cancellationToken))
        {
            statuses.Add(new StatusDistributionModel(
                StatusName: reader.GetString(0),
                Count: reader.GetInt32(1)
            ));
        }

        // Move to 2nd Result Set: Monthly Trends
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                trends.Add(new MonthlyTrendModel(
                    Year: reader.GetInt32(0),
                    Month: reader.GetInt32(1),
                    Volume: reader.GetInt32(2)
                ));
            }
        }

        return (statuses, trends);
    }

    public async Task<(IReadOnlyList<TriageDistributionModel> Triage, int OverrideCount)> GetQueueAndEmergencyStatsAsync(CancellationToken cancellationToken = default)
    {
        var triage = new List<TriageDistributionModel>();
        var overrideCount = 0;

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "dbo.sp_GetQueueAndEmergencyReport";
        command.CommandType = CommandType.StoredProcedure;

        if (command.Connection.State == ConnectionState.Closed)
            await command.Connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // 1st Result Set: Triage levels
        while (await reader.ReadAsync(cancellationToken))
        {
            triage.Add(new TriageDistributionModel(
                LevelName: reader.GetString(0),
                Count: reader.GetInt32(1)
            ));
        }

        // 2nd Result Set: Overrides count
        if (await reader.NextResultAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                overrideCount = reader.GetInt32(0);
            }
        }

        return (triage, overrideCount);
    }

    public async Task<IReadOnlyList<DoctorPerformanceModel>> GetDoctorPerformanceStatsAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<DoctorPerformanceModel>();

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "dbo.sp_GetDoctorPerformanceReport";
        command.CommandType = CommandType.StoredProcedure;

        if (command.Connection.State == ConnectionState.Closed)
            await command.Connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new DoctorPerformanceModel(
                DoctorId: reader.GetInt32(0),
                DoctorName: reader.GetString(1),
                SpecializationName: reader.GetString(2),
                TotalConsultations: reader.GetInt32(3),
                AvgWaitTimeMinutes: Convert.ToDouble(reader.GetValue(4))
            ));
        }

        return list;
    }

    public async Task<(IReadOnlyList<AgeGroupModel> AgeGroups, IReadOnlyList<GenderDistributionModel> Genders)> GetPatientAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var ageGroups = new List<AgeGroupModel>();
        var genders = new List<GenderDistributionModel>();

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "dbo.sp_GetPatientAnalyticsReport";
        command.CommandType = CommandType.StoredProcedure;

        if (command.Connection.State == ConnectionState.Closed)
            await command.Connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // 1st Result Set: Age groups
        while (await reader.ReadAsync(cancellationToken))
        {
            ageGroups.Add(new AgeGroupModel(
                AgeGroup: reader.GetString(0),
                Count: reader.GetInt32(1)
            ));
        }

        // 2nd Result Set: Gender distribution
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                genders.Add(new GenderDistributionModel(
                    Gender: reader.GetString(0),
                    Count: reader.GetInt32(1)
                ));
            }
        }

        return (ageGroups, genders);
    }
}
