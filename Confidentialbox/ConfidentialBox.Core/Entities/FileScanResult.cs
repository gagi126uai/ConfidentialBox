namespace ConfidentialBox.Core.Entities;

public class FileScanResult
{
    public int Id { get; set; }

    public int SharedFileId { get; set; }
    public virtual SharedFile SharedFile { get; set; } = null!;

    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    public bool IsSuspicious { get; set; } = false;

    public string? SuspiciousReason { get; set; }

    public double ThreatScore { get; set; } // 0.0 to 1.0

    // Análisis de contenido
    public bool HasSuspiciousExtension { get; set; } = false;
    public bool HasMaliciousPatterns { get; set; } = false;
    public bool ExceedsSizeThreshold { get; set; } = false;
    public bool UploadedOutsideBusinessHours { get; set; } = false;

    // Metadatos de análisis
    public string FileHash { get; set; } = string.Empty;
    public string DetectedFileType { get; set; } = string.Empty;
    public string AnalysisDetails { get; set; } = string.Empty; // JSON

    // ML Predictions
    public double MalwareProbability { get; set; } = 0.0;
    public double DataExfiltrationProbability { get; set; } = 0.0;
    public double SocialEngineeringProbability { get; set; } = 0.0;
}
