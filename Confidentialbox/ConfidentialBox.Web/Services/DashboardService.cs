using Blazored.LocalStorage;
using ConfidentialBox.Core.DTOs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public class DashboardService : IDashboardService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private const string TOKEN_KEY = "authToken";

    public DashboardService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<DashboardStatsDto?> GetStatsAsync()
    {
        try
        {
            // Obtener el token del almacenamiento local
            var token = await _localStorage.GetItemAsync<string>(TOKEN_KEY);

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            // Hacer la llamada al API
            var response = await _httpClient.GetAsync("api/Dashboard/stats");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en DashboardService.GetStatsAsync: {ex.Message}");
            return null;
        }
    }
}
