namespace ConfidentialBox.Core.Entities;

public class PDFViewerSession
{
    public int Id { get; set; }

    public int SharedFileId { get; set; }
    public virtual SharedFile SharedFile { get; set; } = null!;

    public string? ViewerUserId { get; set; }
    public virtual ApplicationUser? ViewerUser { get; set; }

    public string SessionId { get; set; } = string.Empty; // GUID único por sesión

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public int PageViewCount { get; set; } = 0;

    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; } = 0;

    public TimeSpan TotalViewTime { get; set; }

    public string ViewerIP { get; set; } = string.Empty;

    public string UserAgent { get; set; } = string.Empty;

    // Detección de comportamiento sospechoso
    public int ScreenshotAttempts { get; set; } = 0;

    public int RapidPageChanges { get; set; } = 0; // Cambios de página muy rápidos

    public int PrintAttempts { get; set; } = 0;

    public int CopyAttempts { get; set; } = 0;

    public bool WasBlocked { get; set; } = false;

    public string? BlockReason { get; set; }

    public DateTime? BlockedAt { get; set; }

    // Patrón de lectura
    public string ReadingPattern { get; set; } = string.Empty; // JSON con páginas visitadas y tiempos

    public bool IsSuspicious { get; set; } = false;

    public double SuspicionScore { get; set; } = 0.0;
}