using AppointmentBooking.Database.Entities;

namespace AppointmentBooking.Repository.Interfaces;

public interface INotificationRepository
{
    Task<NotificationTemplate?> GetTemplateByCodeAsync(string templateCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationTemplate>> GetTemplatesAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetUserNotificationsPagedAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(int notificationId, CancellationToken cancellationToken = default);
    Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
