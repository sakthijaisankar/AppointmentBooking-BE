using AppointmentBooking.Database.Entities;

namespace AppointmentBooking.Repository.Interfaces;

public interface IQueueRepository
{
    Task<IReadOnlyList<QueueManagement>> GetActiveQueueAsync(int? doctorId, CancellationToken cancellationToken = default);
    Task<QueueManagement?> GetQueueByIdAsync(int queueId, CancellationToken cancellationToken = default);
    Task<QueueManagement?> GetQueueByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<QueueStatus?> GetQueueStatusByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueueStatus>> GetQueueStatusesAsync(CancellationToken cancellationToken = default);
    Task<QueueManagement> AddQueueEntryAsync(QueueManagement entry, CancellationToken cancellationToken = default);
    Task<int> GetDailyQueueCountAsync(DateTime date, bool isCritical, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
