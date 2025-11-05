using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfidentialBox.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLog>> GetAllAsync(int pageNumber, int pageSize)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetByUserIdAsync(string userId, int pageNumber, int pageSize)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetRecentAsync(int count)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.AuditLogs.CountAsync();
    }

    public async Task AddAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}