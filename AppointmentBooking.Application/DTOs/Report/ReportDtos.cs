using System.Collections.Generic;

namespace AppointmentBooking.Application.DTOs.Report;

public record DashboardSummaryDto(
    int TotalPatients,
    int AppointmentsToday,
    int ActiveQueueCount,
    int AvgWaitTimeMinutes
);

public record StatusDistributionDto(
    string StatusName,
    int Count
);

public record MonthlyTrendDto(
    int Year,
    int Month,
    int Volume
);

public record AppointmentStatsDto(
    List<StatusDistributionDto> Statuses,
    List<MonthlyTrendDto> Trends
);

public record TriageDistributionDto(
    string LevelName,
    int Count
);

public record QueueAndEmergencyStatsDto(
    List<TriageDistributionDto> Triage,
    int OverrideCount
);

public record DoctorPerformanceDto(
    int DoctorId,
    string DoctorName,
    string SpecializationName,
    int TotalConsultations,
    double AvgWaitTimeMinutes
);

public record AgeGroupDto(
    string AgeGroup,
    int Count
);

public record GenderDistributionDto(
    string Gender,
    int Count
);

public record PatientAnalyticsDto(
    List<AgeGroupDto> AgeGroups,
    List<GenderDistributionDto> Genders
);
