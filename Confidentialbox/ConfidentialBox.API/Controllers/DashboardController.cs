using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileAccessRepository _fileAccessRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        IFileRepository fileRepository,
        IFileAccessRepository fileAccessRepository,
        IAuditLogRepository auditLogRepository,
        UserManager<ApplicationUser> userManager)
    {
        _fileRepository = fileRepository;
        _fileAccessRepository = fileAccessRepository;
        _auditLogRepository = auditLogRepository;
        _userManager = userManager;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var totalFiles = await _fileRepository.GetTotalFilesCountAsync();
        var activeFiles = await _fileRepository.GetActiveFilesCountAsync();
        var expiredFiles = await _fileRepository.GetExpiredFilesCountAsync();
        var blockedFiles = await _fileRepository.GetBlockedFilesCountAsync();
        var totalStorage = await _fileRepository.GetTotalStorageBytesAsync();
        var totalAccesses = await _fileAccessRepository.GetTotalAccessesCountAsync();
        var unauthorizedAccesses = await _fileAccessRepository.GetUnauthorizedAccessesCountAsync();
        var totalUsers = _userManager.Users.Count();
        var activeUsers = _userManager.Users.Count(u => u.IsActive);

        var recentLogs = await _auditLogRepository.GetRecentAsync(10);
        var recentActivity = recentLogs.Select(log => new RecentActivityDto
        {
            Action = log.Action,
            UserName = $"{log.User.FirstName} {log.User.LastName}",
            Details = log.NewValues ?? "",
            Timestamp = log.Timestamp
        }).ToList();

        return Ok(new DashboardStatsDto
        {
            TotalFiles = totalFiles,
            ActiveFiles = activeFiles,
            ExpiredFiles = expiredFiles,
            BlockedFiles = blockedFiles,
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalStorageBytes = totalStorage,
            TotalAccesses = totalAccesses,
            UnauthorizedAccesses = unauthorizedAccesses,
            RecentActivity = recentActivity
        });
    }
}

