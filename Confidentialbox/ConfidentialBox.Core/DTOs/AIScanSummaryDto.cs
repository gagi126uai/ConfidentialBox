namespace ConfidentialBox.Core.DTOs;

public class AIScanSummaryDto
{
    public DateTime ExecutedAtUtc { get; set; }
    public int AlertsGenerated { get; set; }
    public int HighRiskProfilesReviewed { get; set; }
    public string Message { get; set; } = string.Empty;
}
