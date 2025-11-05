namespace ConfidentialBox.Core.DTOs;

public class PDFViewerConfigDto
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool HasWatermark { get; set; }
    public string WatermarkText { get; set; } = "CONFIDENTIAL";
    public bool ScreenshotProtectionEnabled { get; set; }
    public bool PrintProtectionEnabled { get; set; }
    public bool CopyProtectionEnabled { get; set; }
    public int MaxViewTimeMinutes { get; set; }
    public bool AIMonitoringEnabled { get; set; }
    public string SessionId { get; set; } = string.Empty;
}
