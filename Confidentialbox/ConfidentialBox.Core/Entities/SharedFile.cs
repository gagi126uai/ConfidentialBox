using System.ComponentModel.DataAnnotations;

namespace ConfidentialBox.Core.Entities;

public class SharedFile
{
    public int Id { get; set; }

    [Required]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    public string EncryptedFileName { get; set; } = string.Empty;

    public string FileExtension { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [Required]
    public string ShareLink { get; set; } = string.Empty;

    public string? MasterPassword { get; set; }

    public string EncryptionKey { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public int? MaxAccessCount { get; set; }

    public int CurrentAccessCount { get; set; } = 0;

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public bool IsBlocked { get; set; } = false;

    public string? BlockReason { get; set; }

    // Usuario que subió el archivo
    public string UploadedByUserId { get; set; } = string.Empty;
    public virtual ApplicationUser UploadedByUser { get; set; } = null!;

    // Configuración de visualizador PDF seguro
    public bool IsPDF { get; set; } = false;
    public bool HasWatermark { get; set; } = false;
    public string WatermarkText { get; set; } = "CONFIDENTIAL";
    public bool ScreenshotProtectionEnabled { get; set; } = true;
    public bool PrintProtectionEnabled { get; set; } = true;
    public bool CopyProtectionEnabled { get; set; } = true;
    public int MaxViewTimeMinutes { get; set; } = 0; // 0 = ilimitado
    public bool AIMonitoringEnabled { get; set; } = true;

    // Almacenamiento seguro
    public byte[]? EncryptedFileContent { get; set; }
    public bool StoreInDatabase { get; set; } = false;
    public bool StoreOnFileSystem { get; set; } = false;
    public string? StoragePath { get; set; }

    // Relaciones
    public virtual ICollection<FileAccess> FileAccesses { get; set; } = new List<FileAccess>();
    public virtual ICollection<FilePermission> FilePermissions { get; set; } = new List<FilePermission>();
}