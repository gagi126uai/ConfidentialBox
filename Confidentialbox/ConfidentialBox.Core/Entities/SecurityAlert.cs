namespace ConfidentialBox.Core.Entities;

public class SecurityAlert
{
    public int Id { get; set; }

    public string AlertType { get; set; } = string.Empty; // BehavioralAnomaly, MaliciousFile, DataExfiltration, etc.

    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical

    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    public int? FileId { get; set; }
    public virtual SharedFile? File { get; set; }

    public string Description { get; set; } = string.Empty;

    public string DetectedPattern { get; set; } = string.Empty;

    public double ConfidenceScore { get; set; } // 0.0 to 1.0

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public bool IsReviewed { get; set; } = false;

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedByUserId { get; set; }

    public string? ReviewNotes { get; set; }

    public bool IsActionTaken { get; set; } = false;

    public string? ActionTaken { get; set; }

    public string RawData { get; set; } = string.Empty; // JSON con detalles
}