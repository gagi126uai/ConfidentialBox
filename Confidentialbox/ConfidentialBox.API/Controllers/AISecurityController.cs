using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using ConfidentialBox.Infrastructure.Repositories;
using ConfidentialBox.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using FileAccess = ConfidentialBox.Core.Entities.FileAccess;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AISecurityController : ControllerBase
{
    private readonly IAISecurityService _aiSecurityService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileRepository _fileRepository;
    private readonly IFileAccessRepository _fileAccessRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IClientContextResolver _clientContextResolver;

    public AISecurityController(
        IAISecurityService aiSecurityService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IFileRepository fileRepository,
        IFileAccessRepository fileAccessRepository,
        IAuditLogRepository auditLogRepository,
        IClientContextResolver clientContextResolver)
    {
        _aiSecurityService = aiSecurityService;
        _context = context;
        _userManager = userManager;
        _fileRepository = fileRepository;
        _fileAccessRepository = fileAccessRepository;
        _auditLogRepository = auditLogRepository;
        _clientContextResolver = clientContextResolver;
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
        [FromQuery] bool? isReviewed = null,
        [FromQuery] string? status = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? alertType = null,
        [FromQuery] string? fileName = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var query = _context.SecurityAlerts
                .Include(a => a.User)
                .Include(a => a.File)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(severity))
            {
                query = query.Where(a => a.Severity == severity);
            }

            if (isReviewed.HasValue)
            {
                query = query.Where(a => a.IsReviewed == isReviewed.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(alertType))
            {
                query = query.Where(a => a.AlertType == alertType);
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                query = query.Where(a => a.File != null && EF.Functions.Like(a.File.OriginalFileName, $"%{fileName}%"));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a =>
                    EF.Functions.Like(a.Description, $"%{search}%") ||
                    EF.Functions.Like(a.DetectedPattern, $"%{search}%") ||
                    EF.Functions.Like(a.AlertType, $"%{search}%") ||
                    (a.File != null && EF.Functions.Like(a.File.OriginalFileName, $"%{search}%")) ||
                    (a.User != null && EF.Functions.Like(a.User.FirstName + " " + a.User.LastName, $"%{search}%")));
            }

            if (from.HasValue)
            {
                query = query.Where(a => a.DetectedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.DetectedAt <= to.Value);
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
                    Status = a.Status,
                    UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : "Usuario desconocido",
                    UserId = a.UserId,
                    FileName = a.File != null ? a.File.OriginalFileName : null,
                    FileId = a.FileId,
                    Description = a.Description,
                    ConfidenceScore = a.ConfidenceScore,
                    DetectedAt = a.DetectedAt,
                    IsReviewed = a.IsReviewed,
                    ActionTaken = a.ActionTaken,
                    ReviewNotes = a.ReviewNotes,
                    ReviewedAt = a.ReviewedAt,
                    ReviewedByUserId = a.ReviewedByUserId,
                    Verdict = a.Verdict,
                    EscalationLevel = a.EscalationLevel
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

    [HttpGet("alerts/summary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AlertSummaryDto>> GetAlertSummary()
    {
        try
        {
            var alerts = await _context.SecurityAlerts.AsNoTracking().ToListAsync();
            var statusCounts = alerts
                .GroupBy(a => string.IsNullOrWhiteSpace(a.Status) ? "Pending" : a.Status)
                .ToDictionary(g => g.Key, g => g.Count());
            var severityCounts = alerts
                .GroupBy(a => string.IsNullOrWhiteSpace(a.Severity) ? "Unknown" : a.Severity)
                .ToDictionary(g => g.Key, g => g.Count());

            var summary = new AlertSummaryDto
            {
                StatusCounts = statusCounts,
                SeverityCounts = severityCounts,
                NewAlerts = alerts.Count(a => !a.IsReviewed)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("alerts/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SecurityAlertDetailDto>> GetAlertDetail(int id)
    {
        try
        {
            var alert = await _context.SecurityAlerts
                .Include(a => a.User)
                .Include(a => a.File)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alert == null)
            {
                return NotFound();
            }

            var actions = await _context.SecurityAlertActions
                .Include(a => a.CreatedByUser)
                .Include(a => a.TargetUser)
                .Include(a => a.TargetFile)
                .Where(a => a.AlertId == id)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            FileAccess? latestAccess = null;
            if (!string.IsNullOrWhiteSpace(alert.UserId))
            {
                latestAccess = await _fileAccessRepository.GetLatestAccessForUserAsync(alert.UserId, alert.FileId);
            }

            var fileDto = alert.File == null
                ? null
                : new FileDto
                {
                    Id = alert.File.Id,
                    OriginalFileName = alert.File.OriginalFileName,
                    FileExtension = alert.File.FileExtension,
                    FileSizeBytes = alert.File.FileSizeBytes,
                    ShareLink = alert.File.ShareLink,
                    UploadedAt = alert.File.UploadedAt,
                    ExpiresAt = alert.File.ExpiresAt,
                    MaxAccessCount = alert.File.MaxAccessCount,
                    CurrentAccessCount = alert.File.CurrentAccessCount,
                    IsBlocked = alert.File.IsBlocked,
                    BlockReason = alert.File.BlockReason,
                    UploadedByUserId = alert.File.UploadedByUserId,
                    UploadedByUserName = alert.File.UploadedByUser != null ? $"{alert.File.UploadedByUser.FirstName} {alert.File.UploadedByUser.LastName}" : string.Empty,
                    HasMasterPassword = !string.IsNullOrWhiteSpace(alert.File.MasterPassword),
                    IsPdf = alert.File.IsPDF,
                    HasWatermark = alert.File.HasWatermark,
                    ScreenshotProtectionEnabled = alert.File.ScreenshotProtectionEnabled,
                    AimMonitoringEnabled = alert.File.AIMonitoringEnabled,
                    IsDeleted = alert.File.IsDeleted,
                    DeletedAt = alert.File.DeletedAt
                };

            var userDto = alert.User == null
                ? null
                : new UserDto
                {
                    Id = alert.User.Id,
                    Email = alert.User.Email ?? string.Empty,
                    FirstName = alert.User.FirstName ?? string.Empty,
                    LastName = alert.User.LastName ?? string.Empty,
                    FullName = $"{alert.User.FirstName} {alert.User.LastName}",
                    PhoneNumber = alert.User.PhoneNumber,
                    IsActive = alert.User.IsActive,
                    IsBlocked = !alert.User.IsActive,
                    BlockReason = alert.User.BlockReason,
                    CreatedAt = alert.User.CreatedAt,
                    LastLoginAt = alert.User.LastLoginAt,
                    Roles = new List<string>()
                };

            var dto = new SecurityAlertDetailDto
            {
                Alert = new SecurityAlertDto
                {
                    Id = alert.Id,
                    AlertType = alert.AlertType,
                    Severity = alert.Severity,
                    Status = alert.Status,
                    UserName = alert.User != null ? $"{alert.User.FirstName} {alert.User.LastName}" : "Usuario desconocido",
                    UserId = alert.UserId,
                    FileName = alert.File?.OriginalFileName,
                    FileId = alert.FileId,
                    Description = alert.Description,
                    ConfidenceScore = alert.ConfidenceScore,
                    DetectedAt = alert.DetectedAt,
                    IsReviewed = alert.IsReviewed,
                    ActionTaken = alert.ActionTaken,
                    ReviewNotes = alert.ReviewNotes,
                    ReviewedAt = alert.ReviewedAt,
                    ReviewedByUserId = alert.ReviewedByUserId,
                    Verdict = alert.Verdict,
                    EscalationLevel = alert.EscalationLevel
                },
                Actions = actions.Select(a => new SecurityAlertActionDto
                {
                    Id = a.Id,
                    ActionType = a.ActionType,
                    Notes = a.Notes,
                    Metadata = a.Metadata,
                    CreatedAt = a.CreatedAt,
                    CreatedByUserId = a.CreatedByUserId,
                    CreatedByUserName = a.CreatedByUser != null ? $"{a.CreatedByUser.FirstName} {a.CreatedByUser.LastName}" : null,
                    TargetUserId = a.TargetUserId,
                    TargetUserName = a.TargetUser != null ? $"{a.TargetUser.FirstName} {a.TargetUser.LastName}" : null,
                    TargetFileId = a.TargetFileId,
                    TargetFileName = a.TargetFile?.OriginalFileName,
                    StatusAfterAction = a.StatusAfterAction
                }).ToList(),
                File = fileDto,
                User = userDto,
                CanBlockFile = true,
                CanBlockUser = true,
                CanEscalateMonitoring = true,
                LatestAccess = latestAccess == null ? null : new FileAccessLogDto
                {
                    Id = latestAccess.Id,
                    AccessedAt = latestAccess.AccessedAt,
                    WasAuthorized = latestAccess.WasAuthorized,
                    Action = latestAccess.Action,
                    AccessedByUserName = latestAccess.AccessedByUser != null ? $"{latestAccess.AccessedByUser.FirstName} {latestAccess.AccessedByUser.LastName}" : null,
                    AccessedByIp = latestAccess.AccessedByIP,
                    UserAgent = latestAccess.UserAgent,
                    DeviceName = latestAccess.DeviceName,
                    DeviceType = latestAccess.DeviceType,
                    OperatingSystem = latestAccess.OperatingSystem,
                    Browser = latestAccess.Browser,
                    Location = latestAccess.Location,
                    Latitude = latestAccess.Latitude,
                    Longitude = latestAccess.Longitude
                }
            };

            return Ok(dto);
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
        if (request == null)
        {
            return BadRequest(new { error = "Solicitud inválida" });
        }

        try
        {
            var alert = await _context.SecurityAlerts
                .Include(a => a.File)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alert == null)
            {
                return NotFound();
            }

            var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var normalizedStatus = NormalizeStatus(request.Status);

            alert.Status = normalizedStatus;
            alert.ReviewNotes = request.ReviewNotes;
            alert.Verdict = request.Verdict;
            alert.ActionTaken = request.ActionTaken;
            alert.IsActionTaken = alert.IsActionTaken || !string.IsNullOrWhiteSpace(request.ActionTaken) || (request.Actions?.Any() == true);
            alert.IsReviewed = !string.Equals(normalizedStatus, "Pending", StringComparison.OrdinalIgnoreCase);
            alert.ReviewedAt = DateTime.UtcNow;
            alert.ReviewedByUserId = reviewerId;

            var actions = new List<SecurityAlertAction>();
            var clientContext = _clientContextResolver.Resolve(HttpContext);

            if (request.Actions != null)
            {
                foreach (var command in request.Actions)
                {
                    if (command == null || string.IsNullOrWhiteSpace(command.ActionType))
                    {
                        continue;
                    }

                    var actionType = command.ActionType.Trim().ToLowerInvariant();
                    switch (actionType)
                    {
                        case "blockfile":
                        {
                            var targetFileId = command.TargetFileId ?? alert.FileId;
                            if (targetFileId.HasValue)
                            {
                                var file = await _fileRepository.GetByIdAsync(targetFileId.Value);
                                if (file != null)
                                {
                                    file.IsBlocked = true;
                                    file.BlockReason = command.Notes ?? request.Verdict ?? "Bloqueado desde alerta";
                                    await _fileRepository.UpdateAsync(file);

                                    actions.Add(new SecurityAlertAction
                                    {
                                        AlertId = alert.Id,
                                        ActionType = "BlockFile",
                                        Notes = command.Notes,
                                        Metadata = JsonSerializer.Serialize(new { file.Id, file.OriginalFileName, file.BlockReason }),
                                        CreatedAt = DateTime.UtcNow,
                                        CreatedByUserId = reviewerId,
                                        TargetFileId = file.Id,
                                        StatusAfterAction = alert.Status
                                    });

                                    await RegisterAuditAsync(reviewerId, "AlertBlockFile", "SharedFile", file.Id.ToString(), command.Notes, clientContext);
                                }
                            }
                            break;
                        }

                        case "blockuser":
                        {
                            var targetUserId = command.TargetUserId ?? alert.UserId;
                            if (!string.IsNullOrEmpty(targetUserId))
                            {
                                var targetUser = await _userManager.FindByIdAsync(targetUserId);
                                if (targetUser != null)
                                {
                                    targetUser.IsActive = false;
                                    await _userManager.UpdateAsync(targetUser);

                                    actions.Add(new SecurityAlertAction
                                    {
                                        AlertId = alert.Id,
                                        ActionType = "BlockUser",
                                        Notes = command.Notes,
                                        Metadata = JsonSerializer.Serialize(new { targetUser.Id, targetUser.Email }),
                                        CreatedAt = DateTime.UtcNow,
                                        CreatedByUserId = reviewerId,
                                        TargetUserId = targetUser.Id,
                                        StatusAfterAction = alert.Status
                                    });

                                    await RegisterAuditAsync(reviewerId, "AlertDeactivateUser", "ApplicationUser", targetUser.Id, command.Notes, clientContext);
                                }
                            }
                            break;
                        }

                        case "increasemonitoring":
                        case "monitoring":
                        case "escalatemonitoring":
                        {
                            var targetUserId = command.TargetUserId ?? alert.UserId;
                            if (!string.IsNullOrEmpty(targetUserId))
                            {
                                await _aiSecurityService.IncreaseMonitoringLevelAsync(targetUserId, command.MonitoringLevel, command.Notes ?? request.ReviewNotes, reviewerId);

                                actions.Add(new SecurityAlertAction
                                {
                                    AlertId = alert.Id,
                                    ActionType = "IncreaseMonitoring",
                                    Notes = command.Notes,
                                    Metadata = JsonSerializer.Serialize(new { targetUserId, Level = command.MonitoringLevel }),
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedByUserId = reviewerId,
                                    TargetUserId = targetUserId,
                                    StatusAfterAction = alert.Status
                                });

                                await RegisterAuditAsync(reviewerId, "AlertMonitoringEscalated", "ApplicationUser", targetUserId, command.Notes, clientContext);
                            }
                            break;
                        }

                        case "message":
                        case "note":
                        {
                            actions.Add(new SecurityAlertAction
                            {
                                AlertId = alert.Id,
                                ActionType = "Message",
                                Notes = command.Notes,
                                Metadata = command.Metadata,
                                CreatedAt = DateTime.UtcNow,
                                CreatedByUserId = reviewerId,
                                TargetUserId = command.TargetUserId,
                                TargetFileId = command.TargetFileId,
                                StatusAfterAction = alert.Status
                            });
                            break;
                        }

                        case "scanfile":
                        {
                            var targetFileId = command.TargetFileId ?? alert.FileId;
                            if (targetFileId.HasValue)
                            {
                                var file = await _fileRepository.GetByIdAsync(targetFileId.Value);
                                if (file != null)
                                {
                                    var analysis = await _aiSecurityService.AnalyzeFileAsync(file, reviewerId ?? string.Empty);
                                    if (analysis.IsThreat || analysis.ThreatScore >= 0.8)
                                    {
                                        file.IsBlocked = true;
                                        file.BlockReason = string.IsNullOrWhiteSpace(command.Notes)
                                            ? $"Bloqueado tras reanálisis: {analysis.Recommendation}"
                                            : command.Notes;
                                        await _fileRepository.UpdateAsync(file);
                                    }

                                    actions.Add(new SecurityAlertAction
                                    {
                                        AlertId = alert.Id,
                                        ActionType = "ScanFile",
                                        Notes = string.IsNullOrWhiteSpace(command.Notes)
                                            ? analysis.Recommendation
                                            : command.Notes,
                                        Metadata = JsonSerializer.Serialize(new { analysis.ThreatScore, analysis.Recommendation, analysis.Threats }),
                                        CreatedAt = DateTime.UtcNow,
                                        CreatedByUserId = reviewerId,
                                        TargetFileId = targetFileId,
                                        StatusAfterAction = alert.Status
                                    });
                                }
                            }
                            break;
                        }

                        case "deletefile":
                        {
                            var targetFileId = command.TargetFileId ?? alert.FileId;
                            if (targetFileId.HasValue)
                            {
                                await _fileRepository.DeleteAsync(targetFileId.Value);
                                actions.Add(new SecurityAlertAction
                                {
                                    AlertId = alert.Id,
                                    ActionType = "DeleteFile",
                                    Notes = string.IsNullOrWhiteSpace(command.Notes)
                                        ? "Archivo purgado desde el centro de alertas"
                                        : command.Notes,
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedByUserId = reviewerId,
                                    TargetFileId = targetFileId,
                                    StatusAfterAction = alert.Status
                                });

                                await RegisterAuditAsync(reviewerId, "AlertDeleteFile", "SharedFile", targetFileId?.ToString(CultureInfo.InvariantCulture), command.Notes, clientContext);
                            }
                            break;
                        }

                        case "escalate":
                        {
                            alert.EscalationLevel = Math.Max(alert.EscalationLevel + 1, command.MonitoringLevel ?? (alert.EscalationLevel + 1));
                            actions.Add(new SecurityAlertAction
                            {
                                AlertId = alert.Id,
                                ActionType = "Escalate",
                                Notes = command.Notes,
                                Metadata = JsonSerializer.Serialize(new { alert.EscalationLevel }),
                                CreatedAt = DateTime.UtcNow,
                                CreatedByUserId = reviewerId,
                                StatusAfterAction = alert.Status
                            });
                            break;
                        }

                        case "escalatetouser":
                        {
                            actions.Add(new SecurityAlertAction
                            {
                                AlertId = alert.Id,
                                ActionType = "EscalateToUser",
                                Notes = command.Notes,
                                Metadata = command.Metadata,
                                CreatedAt = DateTime.UtcNow,
                                CreatedByUserId = reviewerId,
                                TargetUserId = command.TargetUserId,
                                StatusAfterAction = alert.Status
                            });
                            break;
                        }

                        case "status":
                        {
                            var newStatus = NormalizeStatus(command.Notes);
                            alert.Status = newStatus;
                            alert.IsReviewed = !string.Equals(newStatus, "Pending", StringComparison.OrdinalIgnoreCase);
                            actions.Add(new SecurityAlertAction
                            {
                                AlertId = alert.Id,
                                ActionType = "StatusChange",
                                Notes = newStatus,
                                Metadata = command.Metadata,
                                CreatedAt = DateTime.UtcNow,
                                CreatedByUserId = reviewerId,
                                StatusAfterAction = alert.Status
                            });
                            break;
                        }
                    }
                }
            }

            _context.SecurityAlerts.Update(alert);

            if (actions.Count > 0)
            {
                _context.SecurityAlertActions.AddRange(actions);
            }

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

    private async Task RegisterAuditAsync(string? userId, string action, string entityType, string? entityId, string? notes, ClientContext context)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            NewValues = notes,
            Timestamp = DateTime.UtcNow,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            DeviceName = context.DeviceName,
            DeviceType = context.DeviceType,
            OperatingSystem = context.OperatingSystem,
            Browser = context.Browser,
            Location = context.Location,
            Latitude = context.Latitude,
            Longitude = context.Longitude
        };

        await _auditLogRepository.AddAsync(log);
    }

    private static string NormalizeStatus(string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return "Pending";
        }

        return rawStatus.Trim().ToLowerInvariant() switch
        {
            "pending" or "pendiente" => "Pending",
            "investigating" or "investigacion" or "investigando" or "inreview" => "Investigating",
            "escalated" or "escalada" or "escalate" => "Escalated",
            "resolved" or "resuelta" or "solved" => "Resolved",
            "dismissed" or "descartada" or "closed" => "Dismissed",
            _ => "Pending"
        };
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