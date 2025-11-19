using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Entities;

namespace ConfidentialBox.Infrastructure.Repositories;

public interface IUserMessageRepository
{
    Task<List<UserMessage>> GetRecentAsync(string userId, int take = 25, bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<UserMessage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserMessage> AddAsync(UserMessage message, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string userId, int messageId, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserMessage message, CancellationToken cancellationToken = default);
    Task SetArchivedAsync(string userId, int messageId, bool archived, CancellationToken cancellationToken = default);
    Task UnarchiveAllAsync(string userId, CancellationToken cancellationToken = default);
}
