using System;

namespace ConfidentialBox.Core.DTOs;

public class SecurityAlertDto
{
    public int Id { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? FileName { get; set; }
    public int? FileId { get; set; }
    public string Description { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public DateTime DetectedAt { get; set; }
    public bool IsReviewed { get; set; }
    public string? ActionTaken { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? Verdict { get; set; }
    public int EscalationLevel { get; set; }
}
