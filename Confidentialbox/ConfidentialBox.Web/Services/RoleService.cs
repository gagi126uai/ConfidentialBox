using ConfidentialBox.Core.DTOs;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public class RoleService : IRoleService
{
    private readonly HttpClient _httpClient;

    public RoleService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<RoleDto>>("api/roles") ?? new List<RoleDto>();
    }

    public async Task<RoleDto?> CreateRoleAsync(CreateRoleRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/roles", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RoleDto>();
        }
        return null;
    }

    public async Task<bool> DeleteRoleAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/roles/{id}");
        return response.IsSuccessStatusCode;
    }
}