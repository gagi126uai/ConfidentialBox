using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Repositories;

namespace ConfidentialBox.Infrastructure.Services;

public class UserMessageService : IUserMessageService
{
    private readonly IUserMessageRepository _repository;

    public UserMessageService(IUserMessageRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserMessage> CreateAsync(string userId, string subject, string body, string? senderId = null, bool requiresResponse = false, CancellationToken cancellationToken = default)
    {
        await _repository.UnarchiveAllAsync(userId, cancellationToken);

        var message = new UserMessage
        {
            UserId = userId,
            SenderId = senderId,
            Subject = subject,
            Body = body,
            RequiresResponse = requiresResponse,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.AddAsync(message, cancellationToken);
    }

    public Task<List<UserMessage>> GetRecentAsync(string userId, int take = 25, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        return _repository.GetRecentAsync(userId, take, includeArchived, cancellationToken);
    }

    public Task MarkAsReadAsync(string userId, int messageId, CancellationToken cancellationToken = default)
    {
        return _repository.MarkAsReadAsync(userId, messageId, cancellationToken);
    }

    public Task<UserMessage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task UpdateAsync(UserMessage message, CancellationToken cancellationToken = default)
    {
        return _repository.UpdateAsync(message, cancellationToken);
    }

    public Task SetArchivedAsync(string userId, int messageId, bool archived, CancellationToken cancellationToken = default)
    {
        return _repository.SetArchivedAsync(userId, messageId, archived, cancellationToken);
    }

    public Task UnarchiveAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _repository.UnarchiveAllAsync(userId, cancellationToken);
    }
}
