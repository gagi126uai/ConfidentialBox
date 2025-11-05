namespace ConfidentialBox.Core.Entities;

public class RecycleBinItem
{
    public int Id { get; set; }

    public int SharedFileId { get; set; }
    public virtual SharedFile SharedFile { get; set; } = null!;

    public string DeletedByUserId { get; set; } = string.Empty;
    public virtual ApplicationUser DeletedByUser { get; set; } = null!;

    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PermanentDeleteAt { get; set; }

    public bool IsRestored { get; set; } = false;

    public DateTime? RestoredAt { get; set; }

    public string? RestoredByUserId { get; set; }
}