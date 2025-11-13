using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Repositories;

namespace ConfidentialBox.Infrastructure.Services;

public class UserNotificationService : IUserNotificationService
{
    private readonly IUserNotificationRepository _repository;

    public UserNotificationService(IUserNotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserNotification> CreateAsync(string userId, string title, string message, string? severity = null, string? link = null, string? createdByUserId = null, CancellationToken cancellationToken = default)
    {
        var notification = new UserNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Severity = string.IsNullOrWhiteSpace(severity) ? "info" : severity!,
            Link = link,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.AddAsync(notification, cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _repository.GetUnreadCountAsync(userId, cancellationToken);
    }

    public Task<List<UserNotification>> GetRecentAsync(string userId, int take = 10, CancellationToken cancellationToken = default)
    {
        return _repository.GetRecentAsync(userId, take, cancellationToken);
    }

    public Task MarkAsReadAsync(string userId, IEnumerable<int> notificationIds, CancellationToken cancellationToken = default)
    {
        return _repository.MarkAsReadAsync(userId, notificationIds, cancellationToken);
    }
}
