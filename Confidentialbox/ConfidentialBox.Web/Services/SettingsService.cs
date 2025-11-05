using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.DTOs;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public class SettingsService : ISettingsService
{
    private readonly HttpClient _httpClient;

    public SettingsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FileStorageSettings?> GetStorageSettingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<FileStorageSettings>("api/settings/storage");
    }

    public async Task<bool> UpdateStorageSettingsAsync(FileStorageSettings settings)
    {
        var response = await _httpClient.PostAsJsonAsync("api/settings/storage", settings);
        return await ParseOperationResultAsync(response);
    }

    public async Task<EmailServerSettingsDto?> GetEmailServerSettingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<EmailServerSettingsDto>("api/settings/email/server");
    }

    public async Task<bool> UpdateEmailServerSettingsAsync(EmailServerSettingsDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/settings/email/server", request);
        return await ParseOperationResultAsync(response);
    }

    public async Task<EmailNotificationSettingsDto?> GetEmailNotificationSettingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<EmailNotificationSettingsDto>("api/settings/email/notifications");
    }

    public async Task<bool> UpdateEmailNotificationSettingsAsync(EmailNotificationSettingsDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/settings/email/notifications", request);
        return await ParseOperationResultAsync(response);
    }

    private static async Task<bool> ParseOperationResultAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<OperationResultDto>();
        return result?.Success == true;
    }
}
