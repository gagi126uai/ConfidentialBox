using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly ISystemSettingsService _systemSettingsService;

    public SettingsController(ISystemSettingsService systemSettingsService)
    {
        _systemSettingsService = systemSettingsService;
    }

    [HttpGet("storage")]
    public async Task<ActionResult<FileStorageSettings>> GetStorageSettings(CancellationToken cancellationToken)
    {
        var settings = await _systemSettingsService.GetFileStorageSettingsAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPost("storage")]
    public async Task<ActionResult<OperationResultDto>> UpdateStorageSettings([FromBody] FileStorageSettings request, CancellationToken cancellationToken)
    {
        if (!request.StoreInDatabase && !request.StoreOnFileSystem)
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = "Debe seleccionar al menos un destino de almacenamiento"
            });
        }

        if (request.StoreOnFileSystem && string.IsNullOrWhiteSpace(request.FileSystemDirectory))
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = "Debe especificar un directorio cuando se almacena en el sistema de archivos"
            });
        }

        await _systemSettingsService.UpdateFileStorageSettingsAsync(request, GetUserId(), cancellationToken);
        return Ok(new OperationResultDto { Success = true });
    }

    [HttpGet("email/server")]
    public async Task<ActionResult<EmailServerSettingsDto>> GetEmailServerSettings(CancellationToken cancellationToken)
    {
        var settings = await _systemSettingsService.GetEmailServerSettingsAsync(cancellationToken);
        var dto = new EmailServerSettingsDto
        {
            SmtpHost = settings.SmtpHost ?? string.Empty,
            Port = settings.Port,
            UseSsl = settings.UseSsl,
            Username = settings.Username ?? string.Empty,
            FromEmail = settings.FromEmail,
            FromName = settings.FromName,
            HasPassword = !string.IsNullOrWhiteSpace(settings.Password)
        };

        return Ok(dto);
    }

    [HttpPost("email/server")]
    public async Task<ActionResult<OperationResultDto>> UpdateEmailServerSettings([FromBody] EmailServerSettingsDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var settings = new EmailServerSettings
        {
            SmtpHost = request.SmtpHost,
            Port = request.Port,
            UseSsl = request.UseSsl,
            Username = request.Username,
            Password = string.IsNullOrWhiteSpace(request.NewPassword) ? null : request.NewPassword,
            FromEmail = request.FromEmail,
            FromName = request.FromName
        };

        await _systemSettingsService.UpdateEmailServerSettingsAsync(settings, GetUserId(), cancellationToken);
        return Ok(new OperationResultDto { Success = true });
    }

    [HttpGet("email/notifications")]
    public async Task<ActionResult<EmailNotificationSettingsDto>> GetEmailNotificationSettings(CancellationToken cancellationToken)
    {
        var settings = await _systemSettingsService.GetEmailNotificationSettingsAsync(cancellationToken);
        var dto = new EmailNotificationSettingsDto
        {
            SendSecurityAlerts = settings.SendSecurityAlerts,
            SecurityAlertRecipients = settings.SecurityAlertRecipients,
            SendPasswordRecovery = settings.SendPasswordRecovery,
            PasswordRecoveryRecipients = settings.PasswordRecoveryRecipients,
            SendUserInvitations = settings.SendUserInvitations,
            UserInvitationRecipients = settings.UserInvitationRecipients
        };

        return Ok(dto);
    }

    [HttpPost("email/notifications")]
    public async Task<ActionResult<OperationResultDto>> UpdateEmailNotificationSettings([FromBody] EmailNotificationSettingsDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var settings = new EmailNotificationSettings
        {
            SendSecurityAlerts = request.SendSecurityAlerts,
            SecurityAlertRecipients = request.SecurityAlertRecipients ?? string.Empty,
            SendPasswordRecovery = request.SendPasswordRecovery,
            PasswordRecoveryRecipients = request.PasswordRecoveryRecipients ?? string.Empty,
            SendUserInvitations = request.SendUserInvitations,
            UserInvitationRecipients = request.UserInvitationRecipients ?? string.Empty
        };

        await _systemSettingsService.UpdateEmailNotificationSettingsAsync(settings, GetUserId(), cancellationToken);
        return Ok(new OperationResultDto { Success = true });
    }

    [HttpGet("auth/token")]
    public async Task<ActionResult<TokenSettingsDto>> GetTokenSettings(CancellationToken cancellationToken)
    {
        var settings = await _systemSettingsService.GetSecuritySettingsAsync(cancellationToken);
        return Ok(new TokenSettingsDto { TokenLifetimeHours = settings.TokenLifetimeHours });
    }

    [HttpPost("auth/token")]
    public async Task<ActionResult<OperationResultDto>> UpdateTokenSettings([FromBody] TokenSettingsDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var settings = await _systemSettingsService.GetSecuritySettingsAsync(cancellationToken);
        settings.TokenLifetimeHours = request.TokenLifetimeHours;
        await _systemSettingsService.UpdateSecuritySettingsAsync(settings, GetUserId(), cancellationToken);

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpGet("ai/scoring")]
    public async Task<ActionResult<AIScoringSettingsDto>> GetAIScoringSettings(CancellationToken cancellationToken)
    {
        var settings = await _systemSettingsService.GetAIScoringSettingsAsync(cancellationToken);
        return Ok(ToDto(settings));
    }

    [HttpPost("ai/scoring")]
    public async Task<ActionResult<OperationResultDto>> UpdateAIScoringSettings([FromBody] AIScoringSettingsDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.BusinessHoursEnd < request.BusinessHoursStart)
        {
            ModelState.AddModelError(nameof(request.BusinessHoursEnd), "La hora de finalización debe ser mayor o igual a la inicial.");
            return ValidationProblem(ModelState);
        }

        if (request.RecommendationReviewThreshold < request.RecommendationMonitorThreshold ||
            request.RecommendationBlockThreshold < request.RecommendationReviewThreshold)
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = "Los umbrales de recomendación deben cumplir: Monitorear ≤ Revisar ≤ Bloquear."
            });
        }

        if (request.RiskLevelHighThreshold < request.RiskLevelMediumThreshold)
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = "El umbral de riesgo alto debe ser mayor o igual al de riesgo medio."
            });
        }

        if (request.DataExfiltrationHugeFileMB < request.DataExfiltrationLargeFileMB)
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = "El umbral de archivo muy grande debe ser mayor o igual al umbral de archivo grande."
            });
        }

        var settings = new AIScoringSettings
        {
            HighRiskThreshold = request.HighRiskThreshold,
            SuspiciousThreshold = request.SuspiciousThreshold,
            SuspiciousExtensionScore = request.SuspiciousExtensionScore,
            LargeFileScore = request.LargeFileScore,
            OutsideBusinessHoursScore = request.OutsideBusinessHoursScore,
            UnusualUploadsScore = request.UnusualUploadsScore,
            UnusualFileSizeScore = request.UnusualFileSizeScore,
            OutsideHoursBehaviorScore = request.OutsideHoursBehaviorScore,
            UnusualActivityIncrement = request.UnusualActivityIncrement,
            MalwareProbabilityWeight = request.MalwareProbabilityWeight,
            DataExfiltrationWeight = request.DataExfiltrationWeight,
            BusinessHoursStart = request.BusinessHoursStart,
            BusinessHoursEnd = request.BusinessHoursEnd,
            UploadAnomalyMultiplier = request.UploadAnomalyMultiplier,
            FileSizeAnomalyMultiplier = request.FileSizeAnomalyMultiplier,
            MaxFileSizeMB = request.MaxFileSizeMB,
            MalwareSuspiciousExtensionWeight = request.MalwareSuspiciousExtensionWeight,
            MalwareCrackKeywordWeight = request.MalwareCrackKeywordWeight,
            MalwareKeygenKeywordWeight = request.MalwareKeygenKeywordWeight,
            MalwareExecutableWeight = request.MalwareExecutableWeight,
            DataExfiltrationLargeFileMB = request.DataExfiltrationLargeFileMB,
            DataExfiltrationHugeFileMB = request.DataExfiltrationHugeFileMB,
            DataExfiltrationLargeFileWeight = request.DataExfiltrationLargeFileWeight,
            DataExfiltrationHugeFileWeight = request.DataExfiltrationHugeFileWeight,
            DataExfiltrationArchiveWeight = request.DataExfiltrationArchiveWeight,
            DataExfiltrationOffHoursWeight = request.DataExfiltrationOffHoursWeight,
            RecommendationBlockThreshold = request.RecommendationBlockThreshold,
            RecommendationReviewThreshold = request.RecommendationReviewThreshold,
            RecommendationMonitorThreshold = request.RecommendationMonitorThreshold,
            RiskLevelHighThreshold = request.RiskLevelHighThreshold,
            RiskLevelMediumThreshold = request.RiskLevelMediumThreshold,
            SuspiciousExtensions = ParseExtensions(request.SuspiciousExtensions)
        };

        await _systemSettingsService.UpdateAIScoringSettingsAsync(settings, GetUserId(), cancellationToken);
        return Ok(new OperationResultDto { Success = true });
    }

    [HttpGet("pdf-viewer")]
    public async Task<ActionResult<PDFViewerSettingsDto>> GetPdfViewerSettings(CancellationToken cancellationToken)
    {
        var settings = await _systemSettingsService.GetPdfViewerSettingsAsync(cancellationToken);
        return Ok(ToDto(settings));
    }

    [HttpPost("pdf-viewer")]
    public async Task<ActionResult<OperationResultDto>> UpdatePdfViewerSettings([FromBody] PDFViewerSettingsDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var settings = ToSettings(request);
        await _systemSettingsService.UpdatePdfViewerSettingsAsync(settings, GetUserId(), cancellationToken);
        return Ok(new OperationResultDto { Success = true });
    }

    [HttpGet("auth/registration")]
    public async Task<ActionResult<RegistrationSettingsDto>> GetRegistrationSettings(CancellationToken cancellationToken)
    {
        var isEnabled = await _systemSettingsService.IsUserRegistrationEnabledAsync(cancellationToken);
        return Ok(new RegistrationSettingsDto { IsRegistrationEnabled = isEnabled });
    }

    [HttpPost("auth/registration")]
    public async Task<ActionResult<OperationResultDto>> UpdateRegistrationSettings([FromBody] RegistrationSettingsDto request, CancellationToken cancellationToken)
    {
        await _systemSettingsService.UpdateUserRegistrationEnabledAsync(request.IsRegistrationEnabled, GetUserId(), cancellationToken);
        return Ok(new OperationResultDto { Success = true });
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static AIScoringSettingsDto ToDto(AIScoringSettings settings)
    {
        return new AIScoringSettingsDto
        {
            HighRiskThreshold = settings.HighRiskThreshold,
            SuspiciousThreshold = settings.SuspiciousThreshold,
            SuspiciousExtensionScore = settings.SuspiciousExtensionScore,
            LargeFileScore = settings.LargeFileScore,
            OutsideBusinessHoursScore = settings.OutsideBusinessHoursScore,
            UnusualUploadsScore = settings.UnusualUploadsScore,
            UnusualFileSizeScore = settings.UnusualFileSizeScore,
            OutsideHoursBehaviorScore = settings.OutsideHoursBehaviorScore,
            UnusualActivityIncrement = settings.UnusualActivityIncrement,
            MalwareProbabilityWeight = settings.MalwareProbabilityWeight,
            DataExfiltrationWeight = settings.DataExfiltrationWeight,
            BusinessHoursStart = settings.BusinessHoursStart,
            BusinessHoursEnd = settings.BusinessHoursEnd,
            UploadAnomalyMultiplier = settings.UploadAnomalyMultiplier,
            FileSizeAnomalyMultiplier = settings.FileSizeAnomalyMultiplier,
            MaxFileSizeMB = settings.MaxFileSizeMB,
            MalwareSuspiciousExtensionWeight = settings.MalwareSuspiciousExtensionWeight,
            MalwareCrackKeywordWeight = settings.MalwareCrackKeywordWeight,
            MalwareKeygenKeywordWeight = settings.MalwareKeygenKeywordWeight,
            MalwareExecutableWeight = settings.MalwareExecutableWeight,
            DataExfiltrationLargeFileMB = settings.DataExfiltrationLargeFileMB,
            DataExfiltrationHugeFileMB = settings.DataExfiltrationHugeFileMB,
            DataExfiltrationLargeFileWeight = settings.DataExfiltrationLargeFileWeight,
            DataExfiltrationHugeFileWeight = settings.DataExfiltrationHugeFileWeight,
            DataExfiltrationArchiveWeight = settings.DataExfiltrationArchiveWeight,
            DataExfiltrationOffHoursWeight = settings.DataExfiltrationOffHoursWeight,
            RecommendationBlockThreshold = settings.RecommendationBlockThreshold,
            RecommendationReviewThreshold = settings.RecommendationReviewThreshold,
            RecommendationMonitorThreshold = settings.RecommendationMonitorThreshold,
            RiskLevelHighThreshold = settings.RiskLevelHighThreshold,
            RiskLevelMediumThreshold = settings.RiskLevelMediumThreshold,
            SuspiciousExtensions = string.Join(", ", settings.SuspiciousExtensions)
        };
    }

    private static PDFViewerSettingsDto ToDto(PDFViewerSettings settings)
    {
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
            ShowDownloadButton = settings.ShowDownloadButton,
            ShowPrintButton = settings.ShowPrintButton,
            ShowFullscreenButton = settings.ShowFullscreenButton,
            AllowDownload = settings.AllowDownload,
            AllowPrint = settings.AllowPrint,
            AllowCopy = settings.AllowCopy,
            DisableTextSelection = settings.DisableTextSelection,
            DisableContextMenu = settings.DisableContextMenu,
            ForceGlobalWatermark = settings.ForceGlobalWatermark,
            GlobalWatermarkText = settings.GlobalWatermarkText,
            WatermarkOpacity = settings.WatermarkOpacity,
            WatermarkFontSize = settings.WatermarkFontSize,
            WatermarkColor = settings.WatermarkColor,
            MaxViewTimeMinutes = settings.MaxViewTimeMinutes,
            DefaultZoomPercent = settings.DefaultZoomPercent,
            ZoomStepPercent = settings.ZoomStepPercent,
            ViewerPadding = settings.ViewerPadding,
            CustomCss = settings.CustomCss
        };
    }

    private static PDFViewerSettings ToSettings(PDFViewerSettingsDto dto)
    {
        return new PDFViewerSettings
        {
            Theme = dto.Theme,
            AccentColor = dto.AccentColor,
            BackgroundColor = dto.BackgroundColor,
            ToolbarBackgroundColor = dto.ToolbarBackgroundColor,
            ToolbarTextColor = dto.ToolbarTextColor,
            FontFamily = dto.FontFamily,
            ShowToolbar = dto.ShowToolbar,
            ToolbarPosition = dto.ToolbarPosition,
            ShowFileDetails = dto.ShowFileDetails,
            ShowSearch = dto.ShowSearch,
            ShowPageControls = dto.ShowPageControls,
            ShowPageIndicator = dto.ShowPageIndicator,
            ShowDownloadButton = dto.ShowDownloadButton,
            ShowPrintButton = dto.ShowPrintButton,
            ShowFullscreenButton = dto.ShowFullscreenButton,
            AllowDownload = dto.AllowDownload,
            AllowPrint = dto.AllowPrint,
            AllowCopy = dto.AllowCopy,
            DisableTextSelection = dto.DisableTextSelection,
            DisableContextMenu = dto.DisableContextMenu,
            ForceGlobalWatermark = dto.ForceGlobalWatermark,
            GlobalWatermarkText = dto.GlobalWatermarkText,
            WatermarkOpacity = dto.WatermarkOpacity,
            WatermarkFontSize = dto.WatermarkFontSize,
            WatermarkColor = dto.WatermarkColor,
            MaxViewTimeMinutes = dto.MaxViewTimeMinutes,
            DefaultZoomPercent = dto.DefaultZoomPercent,
            ZoomStepPercent = dto.ZoomStepPercent,
            ViewerPadding = dto.ViewerPadding,
            CustomCss = dto.CustomCss
        };
    }

    private static System.Collections.Generic.List<string> ParseExtensions(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new System.Collections.Generic.List<string>();
        }

        var separators = new[] { ',', ';', '\n', '\r' };
        return input
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.StartsWith('.') ? e : "." + e)
            .Select(e => e.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}
