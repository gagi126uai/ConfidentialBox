using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ConfidentialBox.Core.Entities;

namespace ConfidentialBox.Infrastructure.Repositories;

public interface IFileRepository
{
    Task<SharedFile?> GetByIdAsync(int id);
    Task<SharedFile?> GetByShareLinkAsync(string shareLink);
    Task<List<SharedFile>> GetAllAsync(bool includeDeleted = false);
    Task<List<SharedFile>> GetByUserIdAsync(string userId);
    Task<List<SharedFile>> SearchAsync(string? searchTerm, DateTime? uploadedAfter, DateTime? uploadedBefore, string? userId, bool? isBlocked, bool? isDeleted, int pageNumber, int pageSize);
    Task<int> GetTotalCountAsync(string? searchTerm, DateTime? uploadedAfter, DateTime? uploadedBefore, string? userId, bool? isBlocked, bool? isDeleted);
    Task<SharedFile> AddAsync(SharedFile file);
    Task UpdateAsync(SharedFile file);
    Task DeleteAsync(int id);
    Task<int> GetTotalFilesCountAsync();
    Task<int> GetActiveFilesCountAsync();
    Task<int> GetExpiredFilesCountAsync();
    Task<int> GetBlockedFilesCountAsync();
    Task<long> GetTotalStorageBytesAsync();
}