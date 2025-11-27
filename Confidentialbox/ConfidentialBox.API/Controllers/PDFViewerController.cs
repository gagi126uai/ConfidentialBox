
using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using ConfidentialBox.Infrastructure.Repositories;
using ConfidentialBox.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using FileAccess = ConfidentialBox.Core.Entities.FileAccess;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PDFViewerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileRepository _fileRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileAccessRepository _fileAccessRepository;
    private readonly IPDFViewerAIService _pdfViewerAI;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly ISecurePdfSessionPolicyStore _sessionPolicyStore;

    public PDFViewerController(
        ApplicationDbContext context,
        IFileRepository fileRepository,
        IFileStorageService fileStorageService,
        IFileAccessRepository fileAccessRepository,
        IPDFViewerAIService pdfViewerAI,
        ISystemSettingsService systemSettingsService,
        ISecurePdfSessionPolicyStore sessionPolicyStore)
    {
        _context = context;
        _fileRepository = fileRepository;
        _fileStorageService = fileStorageService;
        _fileAccessRepository = fileAccessRepository;
        _pdfViewerAI = pdfViewerAI;
        _systemSettingsService = systemSettingsService;
        _sessionPolicyStore = sessionPolicyStore;
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

            if (file.IsDeleted)
            {
                return Ok(new StartViewerSessionResponse
                {
                    Success = false,
                    ErrorMessage = "Este archivo ya no está disponible"
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

            if (file.MaxAccessCount.HasValue && file.CurrentAccessCount >= file.MaxAccessCount.Value)
            {
                return Ok(new StartViewerSessionResponse
                {
                    Success = false,
                    ErrorMessage = "Se alcanzó el límite de accesos para este archivo"
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

            file.CurrentAccessCount++;
            await _fileRepository.UpdateAsync(file);

            await _fileAccessRepository.AddAsync(new FileAccess
            {
                SharedFileId = file.Id,
                AccessedByUserId = userId,
                AccessedAt = DateTime.UtcNow,
                AccessedByIP = ipAddress,
                Action = "PdfViewerStart",
                WasAuthorized = true,
                UserAgent = userAgent
            });

            var viewerSettings = await _systemSettingsService.GetPdfViewerSettingsAsync(ct);
            var combinedMaxViewTime = CombineMaxViewTime(file.MaxViewTimeMinutes, viewerSettings.MaxViewTimeMinutes);
            var sessionViewerSettings = BuildViewerSettingsDto(viewerSettings, file, combinedMaxViewTime);
            var permissions = BuildViewerPermissionsDto(sessionViewerSettings);
            _sessionPolicyStore.Store(session.SessionId, sessionViewerSettings, combinedMaxViewTime);

            var hasWatermark = viewerSettings.ForceGlobalWatermark || file.HasWatermark;
            var watermarkText = viewerSettings.ForceGlobalWatermark
                ? viewerSettings.GlobalWatermarkText
                : file.WatermarkText;

            var config = new PDFViewerConfigDto
            {
                FileId = file.Id,
                FileName = file.OriginalFileName,
                HasWatermark = hasWatermark,
                WatermarkText = string.IsNullOrWhiteSpace(watermarkText) ? viewerSettings.GlobalWatermarkText : watermarkText,
                WatermarkOpacity = viewerSettings.WatermarkOpacity,
                WatermarkFontSize = viewerSettings.WatermarkFontSize,
                WatermarkColor = viewerSettings.WatermarkColor,
                WatermarkRotationDegrees = viewerSettings.WatermarkRotationDegrees,
                ScreenshotProtectionEnabled = file.ScreenshotProtectionEnabled || sessionViewerSettings.DisableContextMenu,
                PrintProtectionEnabled = file.PrintProtectionEnabled || !sessionViewerSettings.AllowPrint,
                CopyProtectionEnabled = file.CopyProtectionEnabled || !sessionViewerSettings.AllowCopy,
                MaxViewTimeMinutes = combinedMaxViewTime,
                AIMonitoringEnabled = file.AIMonitoringEnabled,
                SessionId = session.SessionId,
                ViewerSettings = sessionViewerSettings,
                EffectivePermissions = permissions
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
                return Ok(new ViewerEventResultDto
                {
                    Blocked = true,
                    Reason = session.BlockReason
                });
            }

            return Ok(new ViewerEventResultDto { Blocked = false });
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

    private static int CombineMaxViewTime(int fileMax, int settingsMax)
    {
        if (fileMax > 0 && settingsMax > 0)
        {
            return Math.Min(fileMax, settingsMax);
        }

        if (fileMax > 0)
        {
            return fileMax;
        }

        if (settingsMax > 0)
        {
            return settingsMax;
        }

        return 0;
    }

    private static PDFViewerSettingsDto BuildViewerSettingsDto(PDFViewerSettings settings, SharedFile file, int combinedMaxViewTime)
    {
        var allowDownload = settings.AllowDownload;
        var allowPrint = settings.AllowPrint && !file.PrintProtectionEnabled;
        var allowCopy = settings.AllowCopy && !file.CopyProtectionEnabled;
        var disableContextMenu = settings.DisableContextMenu || file.ScreenshotProtectionEnabled;
        var disableTextSelection = settings.DisableTextSelection || !allowCopy;

        return new PDFViewerSettingsDto
        {
            Theme = settings.Theme,
            AccentColor = settings.AccentColor,
            BackgroundColor = settings.BackgroundColor,
            ToolbarBackgroundColor = settings.ToolbarBackgroundColor,
            ToolbarTextColor = settings.ToolbarTextColor,
            FontFamily = settings.FontFamily,
            ShowToolbar = settings.ShowToolbar,
            ToolbarPosition = settings.ToolbarPosition,
            ShowFileDetails = settings.ShowFileDetails,
            ShowSearch = settings.ShowSearch,
            ShowPageControls = settings.ShowPageControls,
            ShowPageIndicator = settings.ShowPageIndicator,
            ShowDownloadButton = settings.ShowDownloadButton && allowDownload,
            ShowPrintButton = settings.ShowPrintButton && allowPrint,
            ShowFullscreenButton = settings.ShowFullscreenButton,
            AllowDownload = allowDownload,
            AllowPrint = allowPrint,
            AllowCopy = allowCopy,
            DisableTextSelection = disableTextSelection,
            DisableContextMenu = disableContextMenu,
            ForceGlobalWatermark = settings.ForceGlobalWatermark,
            GlobalWatermarkText = settings.GlobalWatermarkText,
            WatermarkOpacity = settings.WatermarkOpacity,
            WatermarkFontSize = settings.WatermarkFontSize,
            WatermarkColor = settings.WatermarkColor,
            WatermarkRotationDegrees = settings.WatermarkRotationDegrees,
            MaxViewTimeMinutes = combinedMaxViewTime,
            DefaultZoomPercent = settings.DefaultZoomPercent,
            ZoomStepPercent = settings.ZoomStepPercent,
            ViewerPadding = settings.ViewerPadding,
            CustomCss = settings.CustomCss
        };
    }

    private static PDFViewerPermissionsDto BuildViewerPermissionsDto(PDFViewerSettingsDto settings)
    {
        return new PDFViewerPermissionsDto
        {
            ToolbarVisible = settings.ShowToolbar,
            FileDetailsVisible = settings.ShowFileDetails,
            SearchEnabled = settings.ShowSearch && settings.ShowToolbar,
            ZoomControlsEnabled = settings.ShowPageControls && settings.ShowToolbar,
            PageIndicatorEnabled = settings.ShowPageIndicator && settings.ShowToolbar,
            DownloadButtonVisible = settings.ShowDownloadButton && settings.AllowDownload,
            PrintButtonVisible = settings.ShowPrintButton && settings.AllowPrint,
            FullscreenButtonVisible = settings.ShowFullscreenButton,
            DownloadAllowed = settings.AllowDownload,
            PrintAllowed = settings.AllowPrint,
            CopyAllowed = settings.AllowCopy,
            ContextMenuBlocked = settings.DisableContextMenu,
            TextSelectionBlocked = settings.DisableTextSelection,
            WatermarkForced = settings.ForceGlobalWatermark,
            DefaultZoomPercent = settings.DefaultZoomPercent,
            ZoomStepPercent = settings.ZoomStepPercent,
            MaxViewTimeMinutes = settings.MaxViewTimeMinutes
        };
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

    [HttpGet("content/{sessionId}")]
    [AllowAnonymous]
    public async Task<ActionResult<FileContentResponse>> GetContent(string sessionId, CancellationToken ct)
    {
        var session = await _context.PDFViewerSessions
            .Include(s => s.SharedFile)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

        if (session == null)
        {
            return NotFound();
        }

        if (session.WasBlocked)
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = session.BlockReason ?? "La sesión fue bloqueada por motivos de seguridad"
            });
        }

        var file = session.SharedFile ?? await _fileRepository.GetByIdAsync(session.SharedFileId);
        if (file == null)
        {
            return NotFound();
        }

        if (file.IsBlocked)
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = "El archivo fue bloqueado por el administrador"
            });
        }

        if (file.ExpiresAt.HasValue && file.ExpiresAt.Value < DateTime.UtcNow)
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = "El enlace ha expirado"
            });
        }

        byte[] encryptedBytes;
        try
        {
            encryptedBytes = await _fileStorageService.GetFileAsync(file, ct);
        }
        catch (FileNotFoundException)
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = "El archivo cifrado no se encuentra disponible en el almacenamiento"
            });
        }
        catch (InvalidOperationException ex)
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = $"Error al recuperar el archivo: {ex.Message}"
            });
        }

        if (string.IsNullOrWhiteSpace(file.EncryptionKey))
        {
            return Ok(new FileContentResponse
            {
                Success = false,
                ErrorMessage = "El archivo no tiene una clave de cifrado registrada"
            });
        }

        await _fileAccessRepository.AddAsync(new FileAccess
        {
            SharedFileId = file.Id,
            AccessedByUserId = session.ViewerUserId,
            AccessedAt = DateTime.UtcNow,
            AccessedByIP = session.ViewerIP ?? "Unknown",
            Action = "PdfViewerContent",
            WasAuthorized = true,
            UserAgent = session.UserAgent
        });

        return Ok(new FileContentResponse
        {
            Success = true,
            FileName = file.OriginalFileName,
            FileExtension = file.FileExtension,
            EncryptedContent = Convert.ToBase64String(encryptedBytes),
            EncryptionKey = file.EncryptionKey,
            IsPdf = file.IsPDF,
            HasWatermark = file.HasWatermark,
            WatermarkText = file.WatermarkText,
            ScreenshotProtectionEnabled = file.ScreenshotProtectionEnabled,
            PrintProtectionEnabled = file.PrintProtectionEnabled,
            CopyProtectionEnabled = file.CopyProtectionEnabled,
            AimMonitoringEnabled = file.AIMonitoringEnabled,
            MaxViewTimeMinutes = file.MaxViewTimeMinutes
        });
    }

    [HttpPost("end-session/{sessionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> EndSession(string sessionId)
    {
        await _pdfViewerAI.EndSessionAsync(sessionId);
        return Ok();
    }
}

public sealed class EndSessionRequest
{
    public string SessionId { get; set; } = default!;
}

