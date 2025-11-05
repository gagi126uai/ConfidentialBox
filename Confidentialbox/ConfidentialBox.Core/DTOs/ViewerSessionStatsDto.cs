namespace ConfidentialBox.Core.DTOs;

public class ViewerSessionStatsDto
{
    public int SessionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ViewerName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int PageViewCount { get; set; }
    public TimeSpan TotalViewTime { get; set; }
    public int ScreenshotAttempts { get; set; }
    public int PrintAttempts { get; set; }
    public int CopyAttempts { get; set; }
    public bool WasBlocked { get; set; }
    public string? BlockReason { get; set; }
    public double SuspicionScore { get; set; }
    public bool IsSuspicious { get; set; }
}
