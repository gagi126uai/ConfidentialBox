using System;
using Blazored.LocalStorage;
using ConfidentialBox.Core.DTOs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;

namespace ConfidentialBox.Web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly CustomAuthStateProvider _authStateProvider;
    private const string TOKEN_KEY = "authToken";

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage, CustomAuthStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
        {
            if (IsTokenExpired(result.Token))
            {
                await ClearTokenAsync(true);
                return new LoginResponse { Success = false, ErrorMessage = "El token recibido ya expiró. Intenta nuevamente." };
            }

            await _localStorage.SetItemAsync(TOKEN_KEY, result.Token);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", result.Token);
            _authStateProvider.NotifyUserAuthentication(result.Token);
        }

        return result ?? new LoginResponse { Success = false, ErrorMessage = "Error desconocido" };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
        {
            if (IsTokenExpired(result.Token))
            {
                await ClearTokenAsync(true);
                return new LoginResponse { Success = false, ErrorMessage = "El token recibido ya expiró. Intenta nuevamente." };
            }

            await _localStorage.SetItemAsync(TOKEN_KEY, result.Token);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", result.Token);
            _authStateProvider.NotifyUserAuthentication(result.Token);
        }

        return result ?? new LoginResponse { Success = false, ErrorMessage = "Error desconocido" };
    }

    public async Task LogoutAsync()
    {
        await ClearTokenAsync(false);
        _authStateProvider.NotifyUserLogout();
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(TOKEN_KEY);
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            if (IsTokenExpired(token))
            {
                await ClearTokenAsync(true);
                return null;
            }

            return token;
        }
        catch (InvalidOperationException)
        {
            // El almacenamiento local no está disponible durante el prerender
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", request);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<OperationResultDto>();
        return result?.Success == true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", request);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<OperationResultDto>();
        return result?.Success == true;
    }

    public async Task<bool> IsRegistrationEnabledAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<RegistrationSettingsDto>("api/auth/registration-status");
            return result?.IsRegistrationEnabled ?? true;
        }
        catch
        {
            return true;
        }
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return true;
            }

            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo <= DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private async Task ClearTokenAsync(bool notifyAuthProvider)
    {
        try
        {
            await _localStorage.RemoveItemAsync(TOKEN_KEY);
        }
        catch (InvalidOperationException)
        {
            // Ignorar: el almacenamiento puede no estar disponible durante el prerender
        }

        _httpClient.DefaultRequestHeaders.Authorization = null;

        if (notifyAuthProvider)
        {
            _authStateProvider.NotifyUserLogout();
        }
    }
}