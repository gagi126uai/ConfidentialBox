using ConfidentialBox.Core.DTOs;

namespace ConfidentialBox.Web.Services;

public interface IPdfViewerService
{
    Task<StartViewerSessionResponse?> StartSessionAsync(StartViewerSessionRequest request);
    Task<FileContentResponse?> GetSessionContentAsync(string sessionId);
    Task<ViewerEventResultDto?> RecordEventAsync(ViewerEventRequest request);
    Task EndSessionAsync(string sessionId);
}
