namespace ConfidentialBox.Core.DTOs;

public class PDFViewerConfigDto
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool HasWatermark { get; set; }
    public string WatermarkText { get; set; } = "CONFIDENTIAL";
    public double WatermarkOpacity { get; set; } = 0.12;
    public double WatermarkFontSize { get; set; } = 48;
    public string WatermarkColor { get; set; } = "rgba(220,53,69,0.18)";
    public bool ScreenshotProtectionEnabled { get; set; }
    public bool PrintProtectionEnabled { get; set; }
    public bool CopyProtectionEnabled { get; set; }
    public int MaxViewTimeMinutes { get; set; }
    public bool AIMonitoringEnabled { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public PDFViewerSettingsDto ViewerSettings { get; set; } = new();
    public PDFViewerPermissionsDto EffectivePermissions { get; set; } = new();
}
