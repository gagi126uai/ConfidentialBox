using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ConfidentialBox.Core.DTOs;

namespace ConfidentialBox.Web.Services;

public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;

    public NotificationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UserNotificationDto>> GetNotificationsAsync(int take = 10)
    {
        return await _httpClient.GetFromJsonAsync<List<UserNotificationDto>>($"api/notifications?take={take}")
            ?? new List<UserNotificationDto>();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        return await _httpClient.GetFromJsonAsync<int>("api/notifications/unread-count");
    }

    public async Task MarkAsReadAsync(IEnumerable<int> notificationIds)
    {
        var ids = notificationIds?.ToArray() ?? System.Array.Empty<int>();
        if (ids.Length == 0)
        {
            return;
        }

        var response = await _httpClient.PostAsJsonAsync("api/notifications/read", new { notificationIds = ids });
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"No se pudieron actualizar las notificaciones (código {(int)response.StatusCode}). {detail}");
        }
    }

    public async Task DeleteAsync(int notificationId)
    {
        var response = await _httpClient.DeleteAsync($"api/notifications/{notificationId}");
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"No se pudo eliminar la notificación (código {(int)response.StatusCode}). {detail}");
        }
    }
}
