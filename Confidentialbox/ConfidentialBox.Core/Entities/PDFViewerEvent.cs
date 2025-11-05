namespace ConfidentialBox.Core.Entities;

public class PDFViewerEvent
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public virtual PDFViewerSession Session { get; set; } = null!;

    public string EventType { get; set; } = string.Empty; // PageView, ScreenshotAttempt, Print, Copy, etc.

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string EventData { get; set; } = string.Empty; // JSON con detalles

    public int? PageNumber { get; set; }

    public bool WasBlocked { get; set; } = false;
}