using System;

namespace ConfidentialBox.Core.DTOs;

public class UserNotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string Severity { get; set; } = "info";
    public string? Link { get; set; }
}
