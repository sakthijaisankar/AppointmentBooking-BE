namespace AppointmentBooking.Application.DTOs.Queue;

public record CheckInRequestDto
{
    public int AppointmentId { get; init; }
}

public record QueueStatusDto
{
    public int QueueStatusId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record QueueEntryDto
{
    public int QueueId { get; init; }
    public int AppointmentId { get; init; }
    public string AppointmentNumber { get; init; } = string.Empty;
    public int PatientId { get; init; }
    public string PatientCode { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public int DoctorId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string QueueNumber { get; init; } = string.Empty;
    public QueueStatusDto QueueStatus { get; init; } = null!;
    public string PriorityLevelCode { get; init; } = string.Empty;
    public string PriorityLevelName { get; init; } = string.Empty;
    public string PriorityColorHex { get; init; } = string.Empty;
    public int EstimatedWaitTimeMinutes { get; init; }
    public DateTime CheckInTime { get; init; }
    public DateTime? CallingTime { get; init; }
    public DateTime? ConsultationStartTime { get; init; }
    public DateTime? ConsultationEndTime { get; init; }
}

public record PatientQueueStatusDto
{
    public bool IsCheckedIn { get; init; }
    public string? QueueNumber { get; init; }
    public string? StatusCode { get; init; }
    public string? StatusName { get; init; }
    public int PositionAhead { get; init; }
    public int EstimatedWaitTimeMinutes { get; init; }
    public string? DoctorName { get; init; }
}

public record UpdateQueueStatusRequestDto
{
    public string StatusCode { get; init; } = string.Empty;
}
