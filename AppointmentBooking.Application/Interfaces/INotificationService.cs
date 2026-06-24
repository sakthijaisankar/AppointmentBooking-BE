using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.Notification;

namespace AppointmentBooking.Application.Interfaces;

public interface INotificationService
{
    /// <summary>Renders and dispatches a notification for the specified user and event trigger.</summary>
    Task SendNotificationAsync(
        string templateCode, 
        int userId, 
        int? appointmentId, 
        Dictionary<string, string> replacements, 
        CancellationToken cancellationToken = default);

    /// <summary>Gets paged push notifications for the current user.</summary>
    Task<PagedResult<NotificationDto>> GetUserNotificationsPagedAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Gets the count of unread push notifications for the user.</summary>
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Marks a specific notification as read.</summary>
    Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default);

    /// <summary>Marks all user's notifications as read.</summary>
    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Gets all available notification templates (Admin/Staff).</summary>
    Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates a template's definition (Admin/Staff).</summary>
    Task<NotificationTemplateDto> UpdateTemplateAsync(
        string templateCode, 
        UpdateTemplateRequestDto request, 
        CancellationToken cancellationToken = default);
}
