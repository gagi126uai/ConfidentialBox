namespace ConfidentialBox.Core.DTOs;

public class AlertActionCommand
{
    public string ActionType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Metadata { get; set; }
    public int? TargetFileId { get; set; }
    public string? TargetUserId { get; set; }
    public int? MonitoringLevel { get; set; }
}
