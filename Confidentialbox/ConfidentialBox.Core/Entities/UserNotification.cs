using System;

namespace ConfidentialBox.Core.Entities;

public class UserNotification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public string Severity { get; set; } = "info";
    public string? Link { get; set; }
    public string? CreatedByUserId { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ApplicationUser? CreatedByUser { get; set; }
}
