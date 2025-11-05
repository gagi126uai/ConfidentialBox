
using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Infrastructure.Data;
using ConfidentialBox.Infrastructure.Repositories;
using ConfidentialBox.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PDFViewerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileRepository _fileRepository;
    private readonly IPDFViewerAIService _pdfViewerAI;

    public PDFViewerController(
        ApplicationDbContext context,
        IFileRepository fileRepository,
        IPDFViewerAIService pdfViewerAI)
    {
        _context = context;
        _fileRepository = fileRepository;
        _pdfViewerAI = pdfViewerAI;
    }

    [HttpPost("start-session")]
    [AllowAnonymous]
    public async Task<ActionResult<StartViewerSessionResponse>> StartSession(
        [FromBody] StartViewerSessionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ShareLink))
            {
                return Ok(new StartViewerSessionResponse
                {
                    Success = false,
                    ErrorMessage = "Solicitud inválida"
                });
            }

            var file = await _fileRepository.GetByShareLinkAsync(request.ShareLink);
            if (file == null)
            {
                return Ok(new StartViewerSessionResponse
                {
                    Success = false,
                    ErrorMessage = "Archivo no encontrado"
                });
            }

            if (!file.IsPDF)
            {
                return Ok(new StartViewerSessionResponse
                {
                    Success = false,
                    ErrorMessage = "Este archivo no es un PDF"
                });
            }

            if (file.IsBlocked)
            {
                return Ok(new StartViewerSessionResponse
                {
                    Success = false,
                    ErrorMessage = "Este archivo ha sido bloqueado"
                });
            }

            if (file.ExpiresAt.HasValue && file.ExpiresAt.Value < DateTime.UtcNow)
            {
                return Ok(new StartViewerSessionResponse
                {
                    Success = false,
                    ErrorMessage = "Este archivo ha expirado"
                });
            }

            // Contraseña maestra (ideal: comparar HASH)
            if (!string.IsNullOrEmpty(file.MasterPassword))
            {
                if (string.IsNullOrEmpty(request.MasterPassword) || file.MasterPassword != request.MasterPassword)
                {
                    return Ok(new StartViewerSessionResponse
                    {
                        Success = false,
                        ErrorMessage = "Contraseña incorrecta"
                    });
                }
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = GetClientIp();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var session = await _pdfViewerAI.StartSessionAsync(file, userId, ipAddress, userAgent);

            var config = new PDFViewerConfigDto
            {
                FileId = file.Id,
                FileName = file.OriginalFileName,
                HasWatermark = file.HasWatermark,
                WatermarkText = file.WatermarkText,
                ScreenshotProtectionEnabled = file.ScreenshotProtectionEnabled,
                PrintProtectionEnabled = file.PrintProtectionEnabled,
                CopyProtectionEnabled = file.CopyProtectionEnabled,
                MaxViewTimeMinutes = file.MaxViewTimeMinutes,
                AIMonitoringEnabled = file.AIMonitoringEnabled,
                SessionId = session.SessionId
            };

            return Ok(new StartViewerSessionResponse
            {
                Success = true,
                SessionId = session.SessionId,
                Config = config
            });
        }
        catch (Exception ex)
        {
            return Ok(new StartViewerSessionResponse
            {
                Success = false,
                ErrorMessage = $"Error: {ex.Message}"
            });
        }
    }

    [HttpPost("record-event")]
    [AllowAnonymous]
    public async Task<ActionResult> RecordEvent([FromBody] ViewerEventRequest request, CancellationToken ct)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
                return BadRequest(new { error = "Solicitud inválida" });

            await _pdfViewerAI.RecordEventAsync(
                request.SessionId,
                request.EventType,
                request.PageNumber,
                request.EventData);

            var session = await _context.PDFViewerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionId == request.SessionId, ct);

            if (session?.WasBlocked == true)
            {
                return Ok(new { blocked = true, reason = session.BlockReason });
            }

            return Ok(new { blocked = false });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("suspicious-sessions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ViewerSessionStatsDto>>> GetSuspiciousSessions(CancellationToken ct)
    {
        try
        {
            var sessions = await _context.PDFViewerSessions
                .AsNoTracking()
                .Include(s => s.ViewerUser)
                .Include(s => s.SharedFile)
                .Where(s => s.IsSuspicious || s.WasBlocked)
                .OrderByDescending(s => s.SuspicionScore)
                .Take(50)
                .ToListAsync(ct);

            var stats = sessions.Select(s => new ViewerSessionStatsDto
            {
                SessionId = s.Id,
                FileName = s.SharedFile?.OriginalFileName ?? "(desconocido)",
                ViewerName = s.ViewerUser != null
                    ? $"{s.ViewerUser.FirstName} {s.ViewerUser.LastName}"
                    : "Anónimo",
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                PageViewCount = s.PageViewCount,
                TotalViewTime = s.TotalViewTime,
                ScreenshotAttempts = s.ScreenshotAttempts,
                PrintAttempts = s.PrintAttempts,
                CopyAttempts = s.CopyAttempts,
                WasBlocked = s.WasBlocked,
                BlockReason = s.BlockReason,
                SuspicionScore = s.SuspicionScore,
                IsSuspicious = s.IsSuspicious
            }).ToList();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("end-session")]
    [AllowAnonymous]
    public async Task<ActionResult> EndSession([FromBody] EndSessionRequest request, CancellationToken ct)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
                return BadRequest(new { error = "Solicitud inválida" });

            await _pdfViewerAI.EndSessionAsync(request.SessionId);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("session-stats/{fileId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ViewerSessionStatsDto>>> GetSessionStats(int fileId, CancellationToken ct)
    {
        try
        {
            var sessions = await _context.PDFViewerSessions
                .AsNoTracking()
                .Include(s => s.ViewerUser)
                .Include(s => s.SharedFile)
                .Where(s => s.SharedFileId == fileId)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync(ct);

            var stats = sessions.Select(s => new ViewerSessionStatsDto
            {
                SessionId = s.Id,
                FileName = s.SharedFile?.OriginalFileName ?? "(desconocido)",
                ViewerName = s.ViewerUser != null
                    ? $"{s.ViewerUser.FirstName} {s.ViewerUser.LastName}"
                    : "Anónimo",
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                PageViewCount = s.PageViewCount,
                TotalViewTime = s.TotalViewTime,
                ScreenshotAttempts = s.ScreenshotAttempts,
                PrintAttempts = s.PrintAttempts,
                CopyAttempts = s.CopyAttempts,
                WasBlocked = s.WasBlocked,
                BlockReason = s.BlockReason,
                SuspicionScore = s.SuspicionScore,
                IsSuspicious = s.IsSuspicious
            }).ToList();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("update-pdf-settings")]
    [Authorize]
    public async Task<ActionResult> UpdatePDFSettings([FromBody] UpdatePDFSettingsRequest request, CancellationToken ct)
    {
        try
        {
            if (request == null) return BadRequest();

            var file = await _fileRepository.GetByIdAsync(request.FileId);
            if (file == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.UploadedByUserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            file.HasWatermark = request.HasWatermark;
            file.WatermarkText = request.WatermarkText;
            file.ScreenshotProtectionEnabled = request.ScreenshotProtectionEnabled;
            file.PrintProtectionEnabled = request.PrintProtectionEnabled;
            file.CopyProtectionEnabled = request.CopyProtectionEnabled;
            file.MaxViewTimeMinutes = request.MaxViewTimeMinutes;
            file.AIMonitoringEnabled = request.AIMonitoringEnabled;

            await _fileRepository.UpdateAsync(file);

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private string GetClientIp()
    {
        // considerar ForwardedHeadersMiddleware para confiar en estos encabezados
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var first = forwarded.ToString().Split(',').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first)) return first.Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

public sealed class EndSessionRequest
{
    public string SessionId { get; set; } = default!;
}

