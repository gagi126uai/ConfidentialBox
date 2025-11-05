namespace ConfidentialBox.Core.DTOs;

public class FileThreatAnalysisDto
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool IsThreat { get; set; }
    public double ThreatScore { get; set; }
    public List<string> Threats { get; set; } = new();
    public double MalwareProbability { get; set; }
    public double DataExfiltrationProbability { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}