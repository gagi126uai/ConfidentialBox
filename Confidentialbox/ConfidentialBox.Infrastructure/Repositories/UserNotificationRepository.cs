using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfidentialBox.Infrastructure.Repositories;

public class UserNotificationRepository : IUserNotificationRepository
{
    private readonly ApplicationDbContext _context;

    public UserNotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserNotification> AddAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        _context.UserNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync(cancellationToken);
    }

    public async Task<List<UserNotification>> GetRecentAsync(string userId, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _context.UserNotifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(string userId, IEnumerable<int> notificationIds, CancellationToken cancellationToken = default)
    {
        var ids = notificationIds.ToList();
        if (!ids.Any())
        {
            return;
        }

        var notifications = await _context.UserNotifications
            .Where(n => n.UserId == userId && ids.Contains(n.Id))
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
