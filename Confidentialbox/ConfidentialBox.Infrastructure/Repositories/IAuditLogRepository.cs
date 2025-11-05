using ConfidentialBox.Core.Entities;

namespace ConfidentialBox.Infrastructure.Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetAllAsync(int pageNumber, int pageSize);
    Task<List<AuditLog>> GetByUserIdAsync(string userId, int pageNumber, int pageSize);
    Task<List<AuditLog>> GetRecentAsync(int count);
    Task<int> GetTotalCountAsync();
    Task AddAsync(AuditLog auditLog);
}
