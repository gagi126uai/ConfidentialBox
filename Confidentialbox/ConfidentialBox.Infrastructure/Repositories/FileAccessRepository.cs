using System.Linq;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using FileAccess = ConfidentialBox.Core.Entities.FileAccess;

namespace ConfidentialBox.Infrastructure.Repositories;

public class FileAccessRepository : IFileAccessRepository
{
    private readonly ApplicationDbContext _context;

    public FileAccessRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FileAccess>> GetByFileIdAsync(int fileId)
    {
        return await _context.FileAccesses
            .Include(a => a.AccessedByUser)
            .Where(a => a.SharedFileId == fileId)
            .OrderByDescending(a => a.AccessedAt)
            .ToListAsync();
    }

    public async Task<int> GetTotalAccessesCountAsync()
    {
        return await _context.FileAccesses.CountAsync();
    }

    public async Task<int> GetUnauthorizedAccessesCountAsync()
    {
        return await _context.FileAccesses
            .Where(a => !a.WasAuthorized)
            .CountAsync();
    }

    public async Task<FileAccess?> GetLatestAccessForUserAsync(string userId, int? fileId = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var query = _context.FileAccesses
            .Include(a => a.AccessedByUser)
            .Where(a => a.AccessedByUserId == userId);

        if (fileId.HasValue)
        {
            query = query.Where(a => a.SharedFileId == fileId.Value);
        }

        return await query
            .OrderByDescending(a => a.AccessedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(FileAccess fileAccess)
    {
        _context.FileAccesses.Add(fileAccess);
        await _context.SaveChangesAsync();
    }
}