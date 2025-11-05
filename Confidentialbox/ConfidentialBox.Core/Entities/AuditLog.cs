namespace ConfidentialBox.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string IpAddress { get; set; } = string.Empty;

    public string? UserAgent { get; set; }
}
