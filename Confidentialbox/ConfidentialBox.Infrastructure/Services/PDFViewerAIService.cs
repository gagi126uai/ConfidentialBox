using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ConfidentialBox.Infrastructure.Services;

public class PDFViewerAIService : IPDFViewerAIService
{
    private readonly ApplicationDbContext _context;
    private readonly IAISecurityService _aiSecurityService;
    private readonly ISecurePdfSessionPolicyStore _policyStore;

    // Umbrales de detección
    private const int MAX_SCREENSHOT_ATTEMPTS = 3;
    private const int MAX_PRINT_ATTEMPTS = 2;
    private const int RAPID_PAGE_CHANGE_THRESHOLD = 10; // páginas en menos de 10 segundos
    private const double SUSPICIOUS_SCORE_THRESHOLD = 0.6;
    private const double BLOCK_SCORE_THRESHOLD = 0.8;

    public PDFViewerAIService(ApplicationDbContext context, IAISecurityService aiSecurityService, ISecurePdfSessionPolicyStore policyStore)
    {
        _context = context;
        _aiSecurityService = aiSecurityService;
        _policyStore = policyStore;
    }

    public async Task<PDFViewerSession> StartSessionAsync(SharedFile file, string? userId, string ipAddress, string userAgent)
    {
        var session = new PDFViewerSession
        {
            SharedFileId = file.Id,
            ViewerUserId = userId,
            SessionId = Guid.NewGuid().ToString(),
            StartedAt = DateTime.UtcNow,
            ViewerIP = ipAddress,
            UserAgent = userAgent,
            ReadingPattern = "[]"
        };

        _context.PDFViewerSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task RecordEventAsync(string sessionId, string eventType, int? pageNumber, string? eventData)
    {
        var session = await _context.PDFViewerSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null || session.WasBlocked)
            return;

        var rawEventType = string.IsNullOrWhiteSpace(eventType) ? "Unknown" : eventType;
        var eventKey = rawEventType.Trim().ToLowerInvariant();

        _policyStore.TryGet(sessionId, out var policy);
        var policySettings = policy?.Settings;
        var allowPrint = policySettings?.AllowPrint ?? true;
        var allowDownload = policySettings?.AllowDownload ?? true;
        var allowCopy = policySettings?.AllowCopy ?? true;
        var blockContextMenu = policySettings?.DisableContextMenu ?? false;

        // Crear evento
        var viewerEvent = new PDFViewerEvent
        {
            SessionId = session.Id,
            EventType = rawEventType,
            Timestamp = DateTime.UtcNow,
            PageNumber = pageNumber,
            EventData = eventData ?? "{}"
        };

        // Actualizar contadores según el tipo de evento
        switch (eventKey)
        {
            case "screenshotattempt":
                session.ScreenshotAttempts++;
                if (session.ScreenshotAttempts >= MAX_SCREENSHOT_ATTEMPTS)
                {
                    await BlockSessionAsync(session, "Múltiples intentos de captura de pantalla detectados");
                    viewerEvent.WasBlocked = true;
                }
                break;

            case "printattempt":
            case "toolbarprint":
                session.PrintAttempts++;
                if (!allowPrint)
                {
                    viewerEvent.WasBlocked = true;
                }
                if (session.PrintAttempts >= MAX_PRINT_ATTEMPTS)
                {
                    await BlockSessionAsync(session, "Múltiples intentos de impresión detectados");
                    viewerEvent.WasBlocked = true;
                }
                break;

            case "copyattempt":
                session.CopyAttempts++;
                if (!allowCopy)
                {
                    viewerEvent.WasBlocked = true;
                }
                break;

            case "clipboardcopy":
                session.CopyAttempts++;
                session.ClipboardEvents++;
                if (!allowCopy)
                {
                    viewerEvent.WasBlocked = true;
                }
                if (session.ClipboardEvents >= 5)
                {
                    await BlockSessionAsync(session, "Copiado recurrente detectado por IA");
                    viewerEvent.WasBlocked = true;
                }
                break;

            case "downloadattempt":
            case "toolbardownload":
                if (!allowDownload)
                {
                    viewerEvent.WasBlocked = true;
                }
                break;

            case "pageview":
                session.PageViewCount++;
                session.CurrentPage = pageNumber ?? 1;

                // Detectar cambios rápidos de página (posible intento de copiar todo)
                var recentPageViews = await _context.PDFViewerEvents
                    .Where(e => e.SessionId == session.Id &&
                                e.EventType == "PageView" &&
                                e.Timestamp >= DateTime.UtcNow.AddSeconds(-10))
                    .CountAsync();

                if (recentPageViews >= RAPID_PAGE_CHANGE_THRESHOLD)
                {
                    session.RapidPageChanges++;
                    if (session.RapidPageChanges >= 3)
                    {
                        await BlockSessionAsync(session, "Patrón de navegación sospechoso: cambios de página muy rápidos");
                        viewerEvent.WasBlocked = true;
                    }
                }
                break;

            case "visibilityhidden":
                session.VisibilityLossEvents++;
                if (session.VisibilityLossEvents >= 6)
                {
                    await BlockSessionAsync(session, "Múltiples pérdidas de foco detectadas (posible captura)");
                    viewerEvent.WasBlocked = true;
                }
                break;

            case "windowblur":
                session.WindowBlurEvents++;
                break;

            case "fullscreenexit":
                session.FullscreenExitEvents++;
                if (session.FullscreenExitEvents >= 3)
                {
                    await BlockSessionAsync(session, "Salidas de pantalla completa sospechosas");
                    viewerEvent.WasBlocked = true;
                }
                break;

            case "contextmenuopened":
                if (blockContextMenu)
                {
                    viewerEvent.WasBlocked = true;
                }
                break;
        }

        _context.PDFViewerEvents.Add(viewerEvent);

        // Actualizar patrón de lectura
        await UpdateReadingPatternAsync(session, pageNumber);

        // Calcular suspicion score
        session.SuspicionScore = await CalculateSuspicionScoreAsync(session);
        session.IsSuspicious = session.SuspicionScore >= SUSPICIOUS_SCORE_THRESHOLD;

        // Bloquear automáticamente si el score es muy alto
        if (session.SuspicionScore >= BLOCK_SCORE_THRESHOLD && !session.WasBlocked)
        {
            await BlockSessionAsync(session, $"Comportamiento altamente sospechoso detectado por IA (Score: {session.SuspicionScore:F2})");
        }

        await _context.SaveChangesAsync();

        // Analizar comportamiento con IA general
        if (session.IsSuspicious)
        {
            await AnalyzeSessionBehaviorAsync(sessionId);
        }
    }

    public async Task<bool> AnalyzeSessionBehaviorAsync(string sessionId)
    {
        var session = await _context.PDFViewerSessions
            .Include(s => s.SharedFile)
            .Include(s => s.ViewerUser)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
            return false;

        var suspicionScore = await CalculateSuspicionScoreAsync(session);

        if (suspicionScore >= SUSPICIOUS_SCORE_THRESHOLD)
        {
            // Crear alerta de seguridad
            var alertDescription = $"Comportamiento sospechoso en visualización de PDF: " +
                                 $"{session.ScreenshotAttempts} intentos de screenshot, " +
                                 $"{session.PrintAttempts} intentos de impresión, " +
                                 $"{session.RapidPageChanges} cambios rápidos de página";

            var alert = new SecurityAlert
            {
                AlertType = "PDFViewerAnomaly",
                Severity = suspicionScore >= 0.8 ? "High" : "Medium",
                UserId = session.ViewerUserId ?? "Anonymous",
                FileId = session.SharedFileId,
                Description = alertDescription,
                DetectedPattern = "PDF Viewer Behavior Analysis",
                ConfidenceScore = suspicionScore,
                DetectedAt = DateTime.UtcNow
            };

            _context.SecurityAlerts.Add(alert);
            await _context.SaveChangesAsync();

            return true;
        }

        return false;
    }

    public async Task EndSessionAsync(string sessionId)
    {
        var session = await _context.PDFViewerSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session != null && !session.EndedAt.HasValue)
        {
            session.EndedAt = DateTime.UtcNow;
            session.TotalViewTime = session.EndedAt.Value - session.StartedAt;

            // Análisis final de la sesión
            await AnalyzeSessionBehaviorAsync(sessionId);

            await _context.SaveChangesAsync();
        }

        _policyStore.Remove(sessionId);
    }

    public async Task<double> CalculateSuspicionScoreAsync(PDFViewerSession session)
    {
        double score = 0.0;

        // Factor 1: Intentos de screenshot (peso alto)
        if (session.ScreenshotAttempts > 0)
        {
            score += Math.Min(0.4, session.ScreenshotAttempts * 0.15);
        }

        // Factor 2: Intentos de impresión
        if (session.PrintAttempts > 0)
        {
            score += Math.Min(0.3, session.PrintAttempts * 0.15);
        }

        // Factor 3: Cambios rápidos de página (posible intento de copiar)
        if (session.RapidPageChanges > 0)
        {
            score += Math.Min(0.25, session.RapidPageChanges * 0.1);
        }

        // Factor 4: Intentos de copiar texto
        if (session.CopyAttempts > 0)
        {
            score += Math.Min(0.2, session.CopyAttempts * 0.05);
        }

        if (session.ClipboardEvents > 0)
        {
            score += Math.Min(0.2, session.ClipboardEvents * 0.06);
        }

        if (session.WindowBlurEvents > 0)
        {
            score += Math.Min(0.15, session.WindowBlurEvents * 0.04);
        }

        if (session.VisibilityLossEvents > 0)
        {
            score += Math.Min(0.25, session.VisibilityLossEvents * 0.06);
        }

        if (session.FullscreenExitEvents > 0)
        {
            score += Math.Min(0.2, session.FullscreenExitEvents * 0.08);
        }

        // Factor 5: Patrón de lectura anómalo
        var readingPatternScore = await AnalyzeReadingPatternAsync(session);
        score += readingPatternScore * 0.15;

        // Factor 6: Tiempo de visualización inusual
        if (session.EndedAt.HasValue)
        {
            var viewTime = session.TotalViewTime.TotalMinutes;
            var pageCount = session.TotalPages > 0 ? session.TotalPages : 10;
            var avgTimePerPage = viewTime / pageCount;

            // Muy rápido = sospechoso (menos de 5 segundos por página)
            if (avgTimePerPage < 0.083) // 5 segundos
            {
                score += 0.2;
            }
        }

        return Math.Min(1.0, score);
    }

    private async Task BlockSessionAsync(PDFViewerSession session, string reason)
    {
        session.WasBlocked = true;
        session.BlockReason = reason;
        session.BlockedAt = DateTime.UtcNow;
        session.IsSuspicious = true;
        _policyStore.Remove(session.SessionId);

        // Crear alerta crítica
        var alert = new SecurityAlert
        {
            AlertType = "PDFViewerBlocked",
            Severity = "Critical",
            UserId = session.ViewerUserId ?? "Anonymous",
            FileId = session.SharedFileId,
            Description = $"Sesión de visualización de PDF bloqueada automáticamente: {reason}",
            DetectedPattern = "PDF Viewer AI Protection",
            ConfidenceScore = 1.0,
            DetectedAt = DateTime.UtcNow
        };

        _context.SecurityAlerts.Add(alert);
    }

    private async Task UpdateReadingPatternAsync(PDFViewerSession session, int? pageNumber)
    {
        if (!pageNumber.HasValue)
            return;

        try
        {
            var pattern = JsonSerializer.Deserialize<List<PageVisit>>(session.ReadingPattern) ?? new List<PageVisit>();

            pattern.Add(new PageVisit
            {
                Page = pageNumber.Value,
                Timestamp = DateTime.UtcNow
            });

            // Mantener solo los últimos 100 registros
            if (pattern.Count > 100)
            {
                pattern = pattern.Skip(pattern.Count - 100).ToList();
            }

            session.ReadingPattern = JsonSerializer.Serialize(pattern);
        }
        catch
        {
            // Si hay error, inicializar patrón
            session.ReadingPattern = JsonSerializer.Serialize(new List<PageVisit>
            {
                new PageVisit { Page = pageNumber.Value, Timestamp = DateTime.UtcNow }
            });
        }
    }

    private async Task<double> AnalyzeReadingPatternAsync(PDFViewerSession session)
    {
        try
        {
            var pattern = JsonSerializer.Deserialize<List<PageVisit>>(session.ReadingPattern);
            if (pattern == null || pattern.Count < 5)
                return 0.0;

            double anomalyScore = 0.0;

            // Detectar saltos grandes de páginas (lectura no secuencial)
            int largeJumps = 0;
            for (int i = 1; i < pattern.Count; i++)
            {
                var jump = Math.Abs(pattern[i].Page - pattern[i - 1].Page);
                if (jump > 10)
                    largeJumps++;
            }

            if (largeJumps > pattern.Count * 0.5)
            {
                anomalyScore += 0.3; // Más del 50% son saltos grandes
            }

            // Detectar si visita todas las páginas muy rápido
            var uniquePages = pattern.Select(p => p.Page).Distinct().Count();
            if (uniquePages >= session.TotalPages * 0.8 && session.TotalViewTime.TotalMinutes < 2)
            {
                anomalyScore += 0.4; // Visitó casi todo en menos de 2 minutos
            }

            return Math.Min(1.0, anomalyScore);
        }
        catch
        {
            return 0.0;
        }
    }

    private class PageVisit
    {
        public int Page { get; set; }
        public DateTime Timestamp { get; set; }
    }
}