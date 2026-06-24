namespace AppointmentBooking.Repository.Models;

public record DashboardSummaryModel(
    int TotalPatients,
    int AppointmentsToday,
    int ActiveQueueCount,
    int AvgWaitTimeMinutes
);

public record StatusDistributionModel(
    string StatusName,
    int Count
);

public record MonthlyTrendModel(
    int Year,
    int Month,
    int Volume
);

public record TriageDistributionModel(
    string LevelName,
    int Count
);

public record DoctorPerformanceModel(
    int DoctorId,
    string DoctorName,
    string SpecializationName,
    int TotalConsultations,
    double AvgWaitTimeMinutes
);

public record AgeGroupModel(
    string AgeGroup,
    int Count
);

public record GenderDistributionModel(
    string Gender,
    int Count
);
