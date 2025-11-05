using ConfidentialBox.Core.Entities;

namespace ConfidentialBox.Infrastructure.Services;

public interface IPDFViewerAIService
{
    Task<PDFViewerSession> StartSessionAsync(SharedFile file, string? userId, string ipAddress, string userAgent);
    Task RecordEventAsync(string sessionId, string eventType, int? pageNumber, string? eventData);
    Task<bool> AnalyzeSessionBehaviorAsync(string sessionId);
    Task EndSessionAsync(string sessionId);
    Task<double> CalculateSuspicionScoreAsync(PDFViewerSession session);
}
