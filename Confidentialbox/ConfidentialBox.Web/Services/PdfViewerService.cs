using ConfidentialBox.Core.DTOs;
using System.Net.Http.Json;

namespace ConfidentialBox.Web.Services;

public class PdfViewerService : IPdfViewerService
{
    private readonly HttpClient _httpClient;

    public PdfViewerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StartViewerSessionResponse?> StartSessionAsync(StartViewerSessionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/pdfviewer/start-session", request);
        return await response.Content.ReadFromJsonAsync<StartViewerSessionResponse>();
    }

    public async Task<FileContentResponse?> GetSessionContentAsync(string sessionId)
    {
        return await _httpClient.GetFromJsonAsync<FileContentResponse>($"api/pdfviewer/content/{sessionId}");
    }

    public async Task<ViewerEventResultDto?> RecordEventAsync(ViewerEventRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/pdfviewer/record-event", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ViewerEventResultDto>();
    }

    public async Task EndSessionAsync(string sessionId)
    {
        await _httpClient.PostAsync($"api/pdfviewer/end-session/{sessionId}", null);
    }
}
