using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
