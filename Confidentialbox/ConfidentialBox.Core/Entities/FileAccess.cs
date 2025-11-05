namespace ConfidentialBox.Core.Entities;

public class FileAccess
{
    public int Id { get; set; }

    public int SharedFileId { get; set; }
    public virtual SharedFile SharedFile { get; set; } = null!;

    public string? AccessedByUserId { get; set; }
    public virtual ApplicationUser? AccessedByUser { get; set; }

    public string AccessedByIP { get; set; } = string.Empty;

    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    public string Action { get; set; } = string.Empty; // View, Download, Failed

    public bool WasAuthorized { get; set; }

    public string? UserAgent { get; set; }
}