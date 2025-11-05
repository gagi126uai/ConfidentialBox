using Blazored.LocalStorage;
using ConfidentialBox.Core.DTOs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private const string TOKEN_KEY = "authToken";

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
        {
            await _localStorage.SetItemAsync(TOKEN_KEY, result.Token);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", result.Token);
        }

        return result ?? new LoginResponse { Success = false, ErrorMessage = "Error desconocido" };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
        {
            await _localStorage.SetItemAsync(TOKEN_KEY, result.Token);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", result.Token);
        }

        return result ?? new LoginResponse { Success = false, ErrorMessage = "Error desconocido" };
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TOKEN_KEY);
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TOKEN_KEY);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }
}