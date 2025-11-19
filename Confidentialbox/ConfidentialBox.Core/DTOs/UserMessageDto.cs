using System;

namespace ConfidentialBox.Core.DTOs;

public class UserMessageDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string? SenderName { get; set; }
    public bool RequiresResponse { get; set; }
    public bool IsArchived { get; set; }
}
