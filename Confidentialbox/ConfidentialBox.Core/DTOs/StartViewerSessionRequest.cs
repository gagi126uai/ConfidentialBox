namespace ConfidentialBox.Core.DTOs;

public class StartViewerSessionRequest
{
    public string ShareLink { get; set; } = string.Empty;
    public string? MasterPassword { get; set; }
}