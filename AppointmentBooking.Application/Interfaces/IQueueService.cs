using AppointmentBooking.Application.DTOs.Queue;

namespace AppointmentBooking.Application.Interfaces;

public interface IQueueService
{
    Task<QueueEntryDto> CheckInPatientAsync(CheckInRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueueEntryDto>> GetActiveQueueAsync(int? doctorId, CancellationToken cancellationToken = default);
    Task<QueueEntryDto> UpdateQueueStatusAsync(int queueId, UpdateQueueStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<PatientQueueStatusDto> GetPatientQueueStatusAsync(int patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueueStatusDto>> GetQueueStatusesAsync(CancellationToken cancellationToken = default);
}
