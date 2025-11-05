namespace ConfidentialBox.Core.Entities;

public class UserBehaviorProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    // Patrones de comportamiento
    public double AverageFilesPerDay { get; set; }
    public double AverageFileSizeMB { get; set; }
    public TimeSpan TypicalActiveHoursStart { get; set; }
    public TimeSpan TypicalActiveHoursEnd { get; set; }
    public string CommonFileTypes { get; set; } = string.Empty; // JSON array
    public double AverageSessionDuration { get; set; } // minutos

    // Flags de riesgo
    public int UnusualActivityCount { get; set; } = 0;
    public DateTime? LastUnusualActivity { get; set; }
    public double RiskScore { get; set; } = 0.0; // 0.0 to 1.0

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime ProfileCreatedAt { get; set; } = DateTime.UtcNow;
}
