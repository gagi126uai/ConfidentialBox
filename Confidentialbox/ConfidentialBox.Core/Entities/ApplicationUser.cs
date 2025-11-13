using Microsoft.AspNetCore.Identity;

namespace ConfidentialBox.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresMFA { get; set; } = false;
    public string? MFASecret { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? BlockedAt { get; set; }
    public string? BlockedByUserId { get; set; }

    // Relaciones
    public virtual ICollection<SharedFile> UploadedFiles { get; set; } = new List<SharedFile>();
    public virtual ICollection<FileAccess> FileAccesses { get; set; } = new List<FileAccess>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<UserNotification> Notifications { get; set; } = new List<UserNotification>();
    public virtual ICollection<UserMessage> Messages { get; set; } = new List<UserMessage>();
}
