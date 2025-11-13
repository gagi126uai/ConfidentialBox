namespace ConfidentialBox.Core.DTOs;

public class FileAccessResultDto
{
    public bool Success { get; set; }
    public FileDto? File { get; set; }
    public bool RequiresPassword { get; set; }
    public bool Blocked { get; set; }
    public bool BlockedByAi { get; set; }
    public string? BlockReason { get; set; }
    public string? ErrorMessage { get; set; }
}
