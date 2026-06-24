using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.Notification;
using AppointmentBooking.Application.Exceptions;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppointmentBooking.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task SendNotificationAsync(
        string templateCode,
        int userId,
        int? appointmentId,
        Dictionary<string, string> replacements,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch template
        var template = await _notificationRepository.GetTemplateByCodeAsync(templateCode, cancellationToken);
        if (template == null)
        {
            _logger.LogWarning("Notification template with code {TemplateCode} not found or inactive. Skipping.", templateCode);
            return;
        }

        // 2. Fetch recipient user details for contact info
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogError("Recipient User ID {UserId} not found. Cannot send notification.", userId);
            return;
        }

        // 3. Render Title/Subject and Body
        var subjectTemplate = template.SubjectTemplate ?? "Alert from Healwell";
        var title = RenderTemplate(subjectTemplate, replacements);
        var body = RenderTemplate(template.BodyTemplate, replacements);

        // 4. Dispatch based on template channel
        var channel = template.DefaultChannel;

        // If All, we dispatch on each channel
        if (channel.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            await DispatchEmailAsync(userId, user.Email, title, body, appointmentId, cancellationToken);
            await DispatchSmsAsync(userId, user.PhoneNumber, title, body, appointmentId, cancellationToken);
            await DispatchPushAsync(userId, title, body, appointmentId, cancellationToken);
        }
        else if (channel.Equals("Email", StringComparison.OrdinalIgnoreCase))
        {
            await DispatchEmailAsync(userId, user.Email, title, body, appointmentId, cancellationToken);
        }
        else if (channel.Equals("SMS", StringComparison.OrdinalIgnoreCase))
        {
            await DispatchSmsAsync(userId, user.PhoneNumber, title, body, appointmentId, cancellationToken);
        }
        else if (channel.Equals("Push", StringComparison.OrdinalIgnoreCase))
        {
            await DispatchPushAsync(userId, title, body, appointmentId, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unsupported notification channel {Channel} configured on template {TemplateCode}.", channel, templateCode);
        }
    }

    private async Task DispatchEmailAsync(int userId, string email, string subject, string body, int? appointmentId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- [EMAIL DISPATCHED] To: {Email} | Subject: {Subject} | Body: {Body} ---", email, subject, body);

        var notification = new Notification
        {
            UserId = userId,
            AppointmentId = appointmentId,
            Title = subject,
            Body = body,
            Channel = "Email",
            Status = "Sent",
            IsRead = true, // Emails are viewed externally
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddNotificationAsync(notification, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchSmsAsync(int userId, string phoneNumber, string title, string body, int? appointmentId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- [SMS DISPATCHED] To: {Phone} | Message: {Body} ---", phoneNumber, body);

        var notification = new Notification
        {
            UserId = userId,
            AppointmentId = appointmentId,
            Title = title,
            Body = body,
            Channel = "SMS",
            Status = "Sent",
            IsRead = true,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddNotificationAsync(notification, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchPushAsync(int userId, string title, string body, int? appointmentId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- [PUSH NOTIFICATION SENT] To User: {UserId} | Title: {Title} | Message: {Body} ---", userId, title, body);

        var notification = new Notification
        {
            UserId = userId,
            AppointmentId = appointmentId,
            Title = title,
            Body = body,
            Channel = "Push",
            Status = "Sent",
            IsRead = false, // Must be marked read by user in app
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddNotificationAsync(notification, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<NotificationDto>> GetUserNotificationsPagedAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _notificationRepository.GetUserNotificationsPagedAsync(userId, page, pageSize, cancellationToken);
        return new PagedResult<NotificationDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
    }

    public async Task MarkAsReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken)
            ?? throw new NotFoundException($"Notification {notificationId} not found.");

        notification.IsRead = true;
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Fetch all unread push notifications for user
        var (items, _) = await _notificationRepository.GetUserNotificationsPagedAsync(userId, 1, 1000, cancellationToken);
        var unread = items.Where(n => !n.IsRead && n.Channel == "Push");
        foreach (var item in unread)
        {
            item.IsRead = true;
        }
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAllAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _notificationRepository.GetTemplatesAllAsync(cancellationToken);
        return templates.Select(MapToTemplateDto).ToList();
    }

    public async Task<NotificationTemplateDto> UpdateTemplateAsync(
        string templateCode,
        UpdateTemplateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var template = await _notificationRepository.GetTemplateByCodeAsync(templateCode, cancellationToken)
            ?? throw new NotFoundException($"Template with code {templateCode} not found.");

        template.SubjectTemplate = request.SubjectTemplate;
        template.BodyTemplate = request.BodyTemplate;
        template.DefaultChannel = request.DefaultChannel;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await _notificationRepository.SaveChangesAsync(cancellationToken);
        return MapToTemplateDto(template);
    }

    // ─── Render Helper ───────────────────────────────────────────────────────
    private static string RenderTemplate(string templateText, Dictionary<string, string> replacements)
    {
        var rendered = templateText;
        foreach (var r in replacements)
        {
            var keyWithBraces = "{" + r.Key.Trim('{', '}') + "}";
            rendered = rendered.Replace(keyWithBraces, r.Value);
        }
        return rendered;
    }

    // ─── Mappers ─────────────────────────────────────────────────────────────
    private static NotificationDto MapToDto(Notification n) => new(
        NotificationId: n.NotificationId,
        UserId: n.UserId,
        AppointmentId: n.AppointmentId,
        Title: n.Title,
        Body: n.Body,
        Channel: n.Channel,
        Status: n.Status,
        ErrorMessage: n.ErrorMessage,
        IsRead: n.IsRead,
        SentAt: n.SentAt,
        CreatedAt: n.CreatedAt
    );

    private static NotificationTemplateDto MapToTemplateDto(NotificationTemplate t) => new(
        TemplateId: t.TemplateId,
        TemplateCode: t.TemplateCode,
        TemplateName: t.TemplateName,
        SubjectTemplate: t.SubjectTemplate,
        BodyTemplate: t.BodyTemplate,
        DefaultChannel: t.DefaultChannel,
        IsActive: t.IsActive,
        CreatedAt: t.CreatedAt,
        UpdatedAt: t.UpdatedAt
    );
}
