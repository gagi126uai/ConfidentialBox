using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Entities;

namespace ConfidentialBox.Infrastructure.Repositories;

public interface IUserNotificationRepository
{
    Task<List<UserNotification>> GetRecentAsync(string userId, int take = 10, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string userId, IEnumerable<int> notificationIds, CancellationToken cancellationToken = default);
    Task<UserNotification> AddAsync(UserNotification notification, CancellationToken cancellationToken = default);
}
