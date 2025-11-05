namespace ConfidentialBox.Core.DTOs;

public class UpdatePDFSettingsRequest
{
    public int FileId { get; set; }
    public bool HasWatermark { get; set; }
    public string WatermarkText { get; set; } = "CONFIDENTIAL";
    public bool ScreenshotProtectionEnabled { get; set; } = true;
    public bool PrintProtectionEnabled { get; set; } = true;
    public bool CopyProtectionEnabled { get; set; } = true;
    public int MaxViewTimeMinutes { get; set; } = 0;
    public bool AIMonitoringEnabled { get; set; } = true;
}