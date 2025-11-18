namespace ConfidentialBox.Core.DTOs;

public class UserBehaviorAnalysisDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
    public List<string> AnomaliesDetected { get; set; } = new();
    public DateTime LastAnalyzed { get; set; }

    // Métricas de comportamiento
    public double AverageFilesPerDay { get; set; }
    public double CurrentFilesPerDay { get; set; }
    public bool HasUnusualUploadPattern { get; set; }
    public bool HasUnusualAccessPattern { get; set; }
    public bool AccessingOutsideHours { get; set; }
    public bool IsWhitelisted { get; set; }
}