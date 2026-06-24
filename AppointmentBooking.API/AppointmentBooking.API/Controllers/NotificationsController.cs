using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Notification;
using AppointmentBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Get current user's paged push notifications.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await _notificationService.GetUserNotificationsPagedAsync(userId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(result));
    }

    /// <summary>Get count of unread push notifications for the current user.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(ApiResponse<int>.Ok(result));
    }

    /// <summary>Mark a notification as read.</summary>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> MarkAsRead(
        int id,
        CancellationToken cancellationToken = default)
    {
        await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Notification marked as read."));
    }

    /// <summary>Mark all notifications for the current user as read.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User not authenticated.");
        await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(ApiResponse<string>.Ok("All notifications marked as read."));
    }

    /// <summary>Get all notification templates (Staff/Admin only).</summary>
    [HttpGet("templates")]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationTemplateDto>>>> GetTemplates(CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetTemplatesAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotificationTemplateDto>>.Ok(result));
    }

    /// <summary>Update a notification template (Staff/Admin only).</summary>
    [HttpPut("templates/{code}")]
    [Authorize(Policy = AuthPolicies.StaffOnly)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDto>>> UpdateTemplate(
        string code,
        [FromBody] UpdateTemplateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.UpdateTemplateAsync(code, request, cancellationToken);
        return Ok(ApiResponse<NotificationTemplateDto>.Ok(result, "Notification template updated successfully."));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
