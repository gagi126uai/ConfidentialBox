using ConfidentialBox.Core.DTOs;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<UserDto>>("api/users") ?? new List<UserDto>();
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<UserDto>($"api/users/{id}");
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserDto>();
        }
        return null;
    }

    public async Task<bool> ToggleActiveAsync(string id)
    {
        var response = await _httpClient.PutAsync($"api/users/{id}/toggle-active", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateRolesAsync(string id, List<string> roles)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{id}/roles", roles);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/users/{id}");
        return response.IsSuccessStatusCode;
    }
}
