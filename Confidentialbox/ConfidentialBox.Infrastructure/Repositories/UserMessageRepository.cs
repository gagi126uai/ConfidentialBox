using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfidentialBox.Infrastructure.Repositories;

public class UserMessageRepository : IUserMessageRepository
{
    private readonly ApplicationDbContext _context;

    public UserMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserMessage> AddAsync(UserMessage message, CancellationToken cancellationToken = default)
    {
        _context.UserMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<UserMessage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.UserMessages
            .Include(m => m.Sender)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<List<UserMessage>> GetRecentAsync(string userId, int take = 25, CancellationToken cancellationToken = default)
    {
        return await _context.UserMessages
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .Include(m => m.Sender)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(string userId, int messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.UserMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.UserId == userId, cancellationToken);

        if (message == null)
        {
            return;
        }

        message.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
