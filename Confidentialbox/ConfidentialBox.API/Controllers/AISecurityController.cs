using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using ConfidentialBox.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AISecurityController : ControllerBase
{
    private readonly IAISecurityService _aiSecurityService;
    private readonly ApplicationDbContext _context;

    public AISecurityController(
        IAISecurityService aiSecurityService,
        ApplicationDbContext context)
    {
        _aiSecurityService = aiSecurityService;
        _context = context;
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AISecurityDashboardDto>> GetSecurityDashboard()
    {
        try
        {
            var dashboard = await _aiSecurityService.GetSecurityDashboardAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("alerts")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<SecurityAlertDto>>> GetAlerts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? severity = null,
        [FromQuery] bool? isReviewed = null)
    {
        try
        {
            var query = _context.SecurityAlerts
                .Include(a => a.User)
                .Include(a => a.File)
                .AsQueryable();

            if (!string.IsNullOrEmpty(severity))
            {
                query = query.Where(a => a.Severity == severity);
            }

            if (isReviewed.HasValue)
            {
                query = query.Where(a => a.IsReviewed == isReviewed.Value);
            }

            var totalCount = await query.CountAsync();

            var alerts = await query
                .OrderByDescending(a => a.DetectedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new SecurityAlertDto
                {
                    Id = a.Id,
                    AlertType = a.AlertType,
                    Severity = a.Severity,
                    UserName = $"{a.User.FirstName} {a.User.LastName}",
                    FileName = a.File != null ? a.File.OriginalFileName : null,
                    Description = a.Description,
                    ConfidenceScore = a.ConfidenceScore,
                    DetectedAt = a.DetectedAt,
                    IsReviewed = a.IsReviewed,
                    ActionTaken = a.ActionTaken
                })
                .ToListAsync();

            return Ok(new PagedResult<SecurityAlertDto>
            {
                Items = alerts,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("alerts/{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReviewAlert(int id, [FromBody] ReviewAlertRequest request)
    {
        try
        {
            var alert = await _context.SecurityAlerts.FindAsync(id);
            if (alert == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            alert.IsReviewed = true;
            alert.ReviewedAt = DateTime.UtcNow;
            alert.ReviewedByUserId = userId;
            alert.ReviewNotes = request.ReviewNotes;
            alert.IsActionTaken = !string.IsNullOrEmpty(request.ActionTaken);
            alert.ActionTaken = request.ActionTaken;

            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("user-behavior/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserBehaviorAnalysisDto>> AnalyzeUserBehavior(string userId)
    {
        try
        {
            var analysis = await _aiSecurityService.AnalyzeUserBehaviorAsync(userId);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("user-behavior/my-profile")]
    public async Task<ActionResult<UserBehaviorAnalysisDto>> GetMyBehaviorProfile()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var analysis = await _aiSecurityService.AnalyzeUserBehaviorAsync(userId);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("high-risk-users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserBehaviorAnalysisDto>>> GetHighRiskUsers()
    {
        try
        {
            var highRiskProfiles = await _context.UserBehaviorProfiles
                .Include(p => p.User)
                .Where(p => p.RiskScore >= 0.6)
                .OrderByDescending(p => p.RiskScore)
                .Take(10)
                .ToListAsync();

            var analyses = new List<UserBehaviorAnalysisDto>();
            foreach (var profile in highRiskProfiles)
            {
                var analysis = await _aiSecurityService.AnalyzeUserBehaviorAsync(profile.UserId);
                analyses.Add(analysis);
            }

            return Ok(analyses);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("scan-all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AIScanSummaryDto>> ScanAllUsers()
    {
        try
        {
            var summary = await _aiSecurityService.DetectAnomaliesAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("update-behavior-profile")]
    public async Task<ActionResult> UpdateMyBehaviorProfile()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _aiSecurityService.UpdateUserBehaviorProfileAsync(userId);
            return Ok(new { message = "Perfil de comportamiento actualizado" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("file-scan/{fileId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FileScanResult>> GetFileScanResult(int fileId)
    {
        try
        {
            var scanResult = await _context.FileScanResults
                .Where(s => s.SharedFileId == fileId)
                .OrderByDescending(s => s.ScannedAt)
                .FirstOrDefaultAsync();

            if (scanResult == null)
            {
                return NotFound();
            }

            return Ok(scanResult);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("threat-statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetThreatStatistics()
    {
        try
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);

            var stats = new
            {
                TotalAlerts = await _context.SecurityAlerts.CountAsync(),
                AlertsLast30Days = await _context.SecurityAlerts
                    .Where(a => a.DetectedAt >= last30Days)
                    .CountAsync(),
                CriticalAlerts = await _context.SecurityAlerts
                    .Where(a => a.Severity == "Critical")
                    .CountAsync(),
                SuspiciousFiles = await _context.FileScanResults
                    .Where(s => s.IsSuspicious)
                    .CountAsync(),
                HighRiskUsers = await _context.UserBehaviorProfiles
                    .Where(p => p.RiskScore >= 0.7)
                    .CountAsync(),
                AverageRiskScore = await _context.UserBehaviorProfiles
                    .AverageAsync(p => p.RiskScore),
                AlertsByType = await _context.SecurityAlerts
                    .Where(a => a.DetectedAt >= last30Days)
                    .GroupBy(a => a.AlertType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync(),
                AlertsBySeverity = await _context.SecurityAlerts
                    .Where(a => a.DetectedAt >= last30Days)
                    .GroupBy(a => a.Severity)
                    .Select(g => new { Severity = g.Key, Count = g.Count() })
                    .ToListAsync()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}