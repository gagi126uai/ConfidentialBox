using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfidentialBox.Infrastructure.Repositories;

public class FileRepository : IFileRepository
{
    private readonly ApplicationDbContext _context;

    public FileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SharedFile?> GetByIdAsync(int id)
    {
        return await _context.SharedFiles
            .Include(f => f.UploadedByUser)
            .Include(f => f.FilePermissions)
            .ThenInclude(p => p.Role)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<SharedFile?> GetByShareLinkAsync(string shareLink)
    {
        return await _context.SharedFiles
            .Include(f => f.UploadedByUser)
            .Include(f => f.FilePermissions)
            .ThenInclude(p => p.Role)
            .FirstOrDefaultAsync(f => f.ShareLink == shareLink);
    }

    public async Task<List<SharedFile>> GetAllAsync(bool includeDeleted = false)
    {
        var query = _context.SharedFiles
            .Include(f => f.UploadedByUser)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(f => !f.IsDeleted);
        }

        return await query.OrderByDescending(f => f.UploadedAt).ToListAsync();
    }

    public async Task<List<SharedFile>> GetByUserIdAsync(string userId)
    {
        return await _context.SharedFiles
            .Include(f => f.UploadedByUser)
            .Where(f => f.UploadedByUserId == userId && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    public async Task<List<SharedFile>> SearchAsync(string? searchTerm, DateTime? uploadedAfter, DateTime? uploadedBefore, string? userId, bool? isBlocked, bool? isDeleted, int pageNumber, int pageSize)
    {
        var query = _context.SharedFiles
            .Include(f => f.UploadedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(f => f.OriginalFileName.Contains(searchTerm));
        }

        if (uploadedAfter.HasValue)
        {
            query = query.Where(f => f.UploadedAt >= uploadedAfter.Value);
        }

        if (uploadedBefore.HasValue)
        {
            query = query.Where(f => f.UploadedAt <= uploadedBefore.Value);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(f => f.UploadedByUserId == userId);
        }

        if (isBlocked.HasValue)
        {
            query = query.Where(f => f.IsBlocked == isBlocked.Value);
        }

        if (isDeleted.HasValue)
        {
            query = query.Where(f => f.IsDeleted == isDeleted.Value);
        }

        return await query
            .OrderByDescending(f => f.UploadedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? searchTerm, DateTime? uploadedAfter, DateTime? uploadedBefore, string? userId, bool? isBlocked, bool? isDeleted)
    {
        var query = _context.SharedFiles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(f => f.OriginalFileName.Contains(searchTerm));
        }

        if (uploadedAfter.HasValue)
        {
            query = query.Where(f => f.UploadedAt >= uploadedAfter.Value);
        }

        if (uploadedBefore.HasValue)
        {
            query = query.Where(f => f.UploadedAt <= uploadedBefore.Value);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(f => f.UploadedByUserId == userId);
        }

        if (isBlocked.HasValue)
        {
            query = query.Where(f => f.IsBlocked == isBlocked.Value);
        }

        if (isDeleted.HasValue)
        {
            query = query.Where(f => f.IsDeleted == isDeleted.Value);
        }

        return await query.CountAsync();
    }

    public async Task<SharedFile> AddAsync(SharedFile file)
    {
        _context.SharedFiles.Add(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task UpdateAsync(SharedFile file)
    {
        _context.SharedFiles.Update(file);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var file = await _context.SharedFiles.FindAsync(id);
        if (file != null)
        {
            file.IsDeleted = true;
            file.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalFilesCountAsync()
    {
        return await _context.SharedFiles.CountAsync();
    }

    public async Task<int> GetActiveFilesCountAsync()
    {
        return await _context.SharedFiles
            .Where(f => !f.IsDeleted && !f.IsBlocked)
            .CountAsync();
    }

    public async Task<int> GetExpiredFilesCountAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.SharedFiles
            .Where(f => f.ExpiresAt.HasValue && f.ExpiresAt.Value < now)
            .CountAsync();
    }

    public async Task<int> GetBlockedFilesCountAsync()
    {
        return await _context.SharedFiles
            .Where(f => f.IsBlocked)
            .CountAsync();
    }

    public async Task<long> GetTotalStorageBytesAsync()
    {
        return await _context.SharedFiles
            .Where(f => !f.IsDeleted)
            .SumAsync(f => f.FileSizeBytes);
    }
}