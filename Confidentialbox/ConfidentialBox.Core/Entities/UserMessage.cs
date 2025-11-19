using System;

namespace ConfidentialBox.Core.Entities;

public class UserMessage
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? SenderId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public bool RequiresResponse { get; set; }
    public bool IsArchived { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ApplicationUser? Sender { get; set; }
}
