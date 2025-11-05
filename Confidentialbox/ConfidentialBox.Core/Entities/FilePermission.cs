namespace ConfidentialBox.Core.Entities;

public class FilePermission
{
    public int Id { get; set; }

    public int SharedFileId { get; set; }
    public virtual SharedFile SharedFile { get; set; } = null!;

    public string RoleId { get; set; } = string.Empty;
    public virtual ApplicationRole Role { get; set; } = null!;

    public bool CanView { get; set; } = true;
    public bool CanDownload { get; set; } = true;
    public bool CanDelete { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}