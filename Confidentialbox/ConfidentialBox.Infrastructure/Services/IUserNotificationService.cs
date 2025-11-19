using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Entities;

namespace ConfidentialBox.Infrastructure.Services;

public interface IUserNotificationService
{
    Task<UserNotification> CreateAsync(string userId, string title, string message, string? severity = null, string? link = null, string? createdByUserId = null, CancellationToken cancellationToken = default);
    Task<List<UserNotification>> GetRecentAsync(string userId, int take = 10, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string userId, IEnumerable<int> notificationIds, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, int notificationId, CancellationToken cancellationToken = default);
}
