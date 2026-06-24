using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Repository.Implementations;

public class NotificationRepository : INotificationRepository
{
    private readonly AppointmentBookingDbContext _context;

    public NotificationRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationTemplate?> GetTemplateByCodeAsync(string templateCode, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.TemplateCode == templateCode && t.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationTemplate>> GetTemplatesAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .OrderBy(t => t.TemplateName)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetUserNotificationsPagedAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .Include(n => n.Appointment)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && n.Channel == "Push", cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId, cancellationToken);
    }

    public async Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
