using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IUserNotificationService _notificationService;

    public NotificationsController(IUserNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserNotificationDto>>> GetMyNotifications([FromQuery] int take = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        take = Math.Clamp(take, 1, 50);
        var notifications = await _notificationService.GetRecentAsync(userId, take);
        var dtos = notifications.Select(n => new UserNotificationDto
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            CreatedAt = n.CreatedAt,
            IsRead = n.IsRead,
            Severity = n.Severity,
            Link = n.Link
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(count);
    }

    public sealed class MarkNotificationsRequest
    {
        public List<int> NotificationIds { get; set; } = new();
    }

    [HttpPost("read")]
    public async Task<ActionResult> MarkNotificationsAsRead([FromBody] MarkNotificationsRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var ids = request?.NotificationIds ?? new List<int>();
        await _notificationService.MarkAsReadAsync(userId, ids, HttpContext.RequestAborted);
        return NoContent();
    }
}
