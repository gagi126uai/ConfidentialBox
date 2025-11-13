using ConfidentialBox.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public interface INotificationService
{
    Task<List<UserNotificationDto>> GetNotificationsAsync(int take = 10);
    Task<int> GetUnreadCountAsync();
    Task MarkAsReadAsync(IEnumerable<int> notificationIds);
}
