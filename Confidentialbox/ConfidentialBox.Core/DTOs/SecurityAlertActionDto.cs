using System;

namespace ConfidentialBox.Core.DTOs;

public class SecurityAlertActionDto
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetUserName { get; set; }
    public int? TargetFileId { get; set; }
    public string? TargetFileName { get; set; }
    public string? StatusAfterAction { get; set; }
}
