namespace ConfidentialBox.Core.DTOs;

public class StartViewerSessionResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public PDFViewerConfigDto? Config { get; set; }
}