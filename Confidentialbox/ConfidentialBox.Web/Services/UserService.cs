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

    public async Task<OperationResultDto> DeleteUserAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/users/{id}");
        if (response.IsSuccessStatusCode)
        {
            return new OperationResultDto { Success = true };
        }

        try
        {
            var result = await response.Content.ReadFromJsonAsync<OperationResultDto>();
            if (result != null)
            {
                result.Success = false;
                return result;
            }
        }
        catch
        {
            // Ignored - fall back to raw message
        }

        var message = await response.Content.ReadAsStringAsync();
        return new OperationResultDto
        {
            Success = false,
            Error = string.IsNullOrWhiteSpace(message) ? "No fue posible eliminar el usuario." : message
        };
    }

    public async Task<UserDto?> UpdateUserAsync(string id, UpdateUserProfileRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{id}/profile", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    public async Task<UserProfileDto?> GetMyProfileAsync()
    {
        return await _httpClient.GetFromJsonAsync<UserProfileDto>("api/users/me");
    }

    public async Task<OperationResultDto> UpdateMyProfileAsync(SelfProfileUpdateRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync("api/users/me/profile", request);
        return await response.Content.ReadFromJsonAsync<OperationResultDto>()
            ?? new OperationResultDto { Success = response.IsSuccessStatusCode };
    }

    public async Task<OperationResultDto> ChangeMyPasswordAsync(ChangeOwnPasswordRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync("api/users/me/password", request);
        return await response.Content.ReadFromJsonAsync<OperationResultDto>()
            ?? new OperationResultDto { Success = response.IsSuccessStatusCode };
    }

    public async Task<List<UserMessageDto>> GetMyMessagesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<UserMessageDto>>("api/users/me/messages")
            ?? new List<UserMessageDto>();
    }

    public async Task MarkMyMessageAsReadAsync(int messageId)
    {
        await _httpClient.PostAsync($"api/users/me/messages/{messageId}/read", null);
    }

    public async Task<OperationResultDto> SendMessageAsync(string userId, CreateUserMessageRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/users/{userId}/messages", request);
        return await response.Content.ReadFromJsonAsync<OperationResultDto>()
            ?? new OperationResultDto { Success = response.IsSuccessStatusCode };
    }

    public async Task<OperationResultDto> ChangeUserPasswordAsync(string id, ChangeUserPasswordRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{id}/password", request);
        return await response.Content.ReadFromJsonAsync<OperationResultDto>()
            ?? new OperationResultDto { Success = response.IsSuccessStatusCode };
    }
}
