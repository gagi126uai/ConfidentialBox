namespace ConfidentialBox.Core.DTOs;

public class PDFViewerPermissionsDto
{
    public bool ToolbarVisible { get; set; }
    public bool FileDetailsVisible { get; set; }
    public bool SearchEnabled { get; set; }
    public bool ZoomControlsEnabled { get; set; }
    public bool PageIndicatorEnabled { get; set; }
    public bool DownloadButtonVisible { get; set; }
    public bool PrintButtonVisible { get; set; }
    public bool FullscreenButtonVisible { get; set; }
    public bool DownloadAllowed { get; set; }
    public bool PrintAllowed { get; set; }
    public bool CopyAllowed { get; set; }
    public bool ContextMenuBlocked { get; set; }
    public bool TextSelectionBlocked { get; set; }
    public bool WatermarkForced { get; set; }
    public int DefaultZoomPercent { get; set; }
    public int ZoomStepPercent { get; set; }
    public int MaxViewTimeMinutes { get; set; }
}
