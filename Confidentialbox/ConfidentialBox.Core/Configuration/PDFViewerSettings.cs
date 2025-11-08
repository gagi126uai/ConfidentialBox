namespace ConfidentialBox.Core.Configuration;

public class PDFViewerSettings
{
    public string Theme { get; set; } = "dark";
    public string AccentColor { get; set; } = "#f97316";
    public string BackgroundColor { get; set; } = "#0f172a";
    public string ToolbarBackgroundColor { get; set; } = "#111827";
    public string ToolbarTextColor { get; set; } = "#f9fafb";
    public string FontFamily { get; set; } = "'Inter', 'Segoe UI', sans-serif";
    public bool ShowToolbar { get; set; } = true;
    public string ToolbarPosition { get; set; } = "top";
    public bool ShowFileDetails { get; set; } = true;
    public bool ShowSearch { get; set; } = true;
    public bool ShowPageControls { get; set; } = true;
    public bool ShowPageIndicator { get; set; } = true;
    public bool ShowDownloadButton { get; set; } = false;
    public bool ShowPrintButton { get; set; } = false;
    public bool ShowFullscreenButton { get; set; } = true;
    public bool AllowDownload { get; set; } = false;
    public bool AllowPrint { get; set; } = false;
    public bool AllowCopy { get; set; } = false;
    public bool DisableTextSelection { get; set; } = true;
    public bool DisableContextMenu { get; set; } = true;
    public bool ForceGlobalWatermark { get; set; } = false;
    public string GlobalWatermarkText { get; set; } = "CONFIDENTIAL";
    public double WatermarkOpacity { get; set; } = 0.12;
    public double WatermarkFontSize { get; set; } = 48;
    public string WatermarkColor { get; set; } = "rgba(220,53,69,0.18)";
    public int MaxViewTimeMinutes { get; set; } = 0;
    public int DefaultZoomPercent { get; set; } = 110;
    public int ZoomStepPercent { get; set; } = 15;
    public string ViewerPadding { get; set; } = "1.5rem";
    public string CustomCss { get; set; } = string.Empty;
}
