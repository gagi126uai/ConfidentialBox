namespace ConfidentialBox.Core.DTOs;

public class FileContentResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? BlockReason { get; set; }
    public bool BlockedByAi { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string EncryptedContent { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public bool IsPdf { get; set; }
    public bool HasWatermark { get; set; }
    public string WatermarkText { get; set; } = "CONFIDENTIAL";
    public bool ScreenshotProtectionEnabled { get; set; }
    public bool PrintProtectionEnabled { get; set; }
    public bool CopyProtectionEnabled { get; set; }
    public bool AimMonitoringEnabled { get; set; }
    public int MaxViewTimeMinutes { get; set; }
}
