namespace AppointmentBooking.Application.DTOs.Notification;

public record NotificationDto(
    int NotificationId,
    int UserId,
    int? AppointmentId,
    string Title,
    string Body,
    string Channel,
    string Status,
    string? ErrorMessage,
    bool IsRead,
    DateTime? SentAt,
    DateTime CreatedAt
);

public record NotificationTemplateDto(
    int TemplateId,
    string TemplateCode,
    string TemplateName,
    string? SubjectTemplate,
    string BodyTemplate,
    string DefaultChannel,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpdateTemplateRequestDto(
    string? SubjectTemplate,
    string BodyTemplate,
    string DefaultChannel,
    bool IsActive
);
