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

    public async Task<RegistrationSettingsDto?> GetRegistrationSettingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<RegistrationSettingsDto>("api/settings/auth/registration");
    }

    public async Task<bool> UpdateRegistrationSettingsAsync(bool isEnabled)
    {
        var response = await _httpClient.PostAsJsonAsync("api/settings/auth/registration", new RegistrationSettingsDto
        {
            IsRegistrationEnabled = isEnabled
        });
        return await ParseOperationResultAsync(response);
    }

    public async Task<TokenSettingsDto?> GetTokenSettingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<TokenSettingsDto>("api/settings/auth/token");
    }

    public async Task<bool> UpdateTokenSettingsAsync(TokenSettingsDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/settings/auth/token", request);
        return await ParseOperationResultAsync(response);
    }

    public async Task<AIScoringSettingsDto?> GetAIScoringSettingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<AIScoringSettingsDto>("api/settings/ai/scoring");
    }

    public async Task<bool> UpdateAIScoringSettingsAsync(AIScoringSettingsDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/settings/ai/scoring", request);
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
