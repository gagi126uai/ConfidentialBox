using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System;
using ConfidentialBox.Infrastructure.Repositories;
using ConfidentialBox.Infrastructure.Services;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IClientContextResolver _clientContextResolver;
    private readonly IUserNotificationService _userNotificationService;
    private readonly IUserMessageService _userMessageService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        IAuditLogRepository auditLogRepository,
        IClientContextResolver clientContextResolver,
        IUserNotificationService userNotificationService,
        IUserMessageService userMessageService)
    {
        _userManager = userManager;
        _auditLogRepository = auditLogRepository;
        _clientContextResolver = clientContextResolver;
        _userNotificationService = userNotificationService;
        _userMessageService = userMessageService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                IsBlocked = user.IsBlocked,
                BlockReason = user.BlockReason,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles.ToList()
            });
        }

        return Ok(userDtos);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            IsBlocked = user.IsBlocked,
            BlockReason = user.BlockReason,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList()
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest("El email ya está registrado");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var normalizedRoles = request.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(1)
            .ToList();

        if (normalizedRoles.Count == 0)
        {
            normalizedRoles.Add("User");
        }

        await _userManager.AddToRolesAsync(user, normalizedRoles);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            IsBlocked = user.IsBlocked,
            BlockReason = user.BlockReason,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        });
    }

    [HttpPut("{id}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OperationResultDto>> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new OperationResultDto { Success = false, Error = "Usuario no encontrado" });
        }

        if (user.IsActive)
        {
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                return BadRequest(new OperationResultDto
                {
                    Success = false,
                    Error = "Las cuentas con rol Admin no pueden ser desactivadas"
                });
            }
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var context = _clientContextResolver.Resolve(HttpContext);
        await RegisterAuditAsync(actorId, user.IsActive ? "UserActivated" : "UserDeactivated", "ApplicationUser", id, null, context);

        var title = user.IsActive ? "Tu cuenta fue activada" : "Tu cuenta fue desactivada";
        var message = user.IsActive
            ? "Un administrador reactivó tu acceso a ConfidentialBox."
            : "Tu cuenta ha sido desactivada temporalmente. Comunícate con soporte si necesitas más información.";
        var severity = user.IsActive ? "success" : "warning";
        await _userNotificationService.CreateAsync(user.Id, title, message, severity, null, actorId);

        if (!user.IsActive)
        {
            await _userMessageService.CreateAsync(user.Id, "Cuenta desactivada", "Se desactivó tu cuenta por decisión administrativa. Responde este mensaje si necesitas asistencia.", actorId, requiresResponse: true);
        }

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpPut("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateRoles(string id, [FromBody] List<string> roles)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (roles == null || roles.Count == 0)
        {
            return BadRequest("Debe seleccionar un rol principal.");
        }

        var normalizedRoles = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(1)
            .ToList();

        if (normalizedRoles.Count == 0)
        {
            return BadRequest("Debe seleccionar un rol principal.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRolesAsync(user, normalizedRoles);

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var context = _clientContextResolver.Resolve(HttpContext);
        await RegisterAuditAsync(actorId, "UserRolesUpdated", "ApplicationUser", id, string.Join(",", normalizedRoles), context);

        return Ok();
    }

    [HttpPut("{id}/password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OperationResultDto>> ChangePassword(string id, [FromBody] ChangeUserPasswordRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new OperationResultDto { Success = false, Error = "La nueva contraseña es obligatoria" });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new OperationResultDto { Success = false, Error = "Usuario no encontrado" });
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            var detail = string.Join("; ", resetResult.Errors.Select(e => e.Description));
            return BadRequest(new OperationResultDto { Success = false, Detail = detail });
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var context = _clientContextResolver.Resolve(HttpContext);
        await RegisterAuditAsync(actorId, "UserPasswordReset", "ApplicationUser", id, request.Reason, context);

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpPut("{id}/profile")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> UpdateProfile(string id, [FromBody] UpdateUserProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!request.IsActive && currentRoles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Las cuentas con rol Admin no pueden ser desactivadas.");
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var context = _clientContextResolver.Resolve(HttpContext);
        var previousActive = user.IsActive;
        var previousBlocked = user.IsBlocked;
        var previousBlockReason = user.BlockReason;

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim();
            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(normalizedEmail);
                if (existing != null && existing.Id != user.Id)
                {
                    return BadRequest("El email ya está asignado a otro usuario.");
                }

                user.Email = normalizedEmail;
                user.UserName = normalizedEmail;
            }
        }

        if (request.IsActive != user.IsActive)
        {
            user.IsActive = request.IsActive;
        }

        if (request.IsBlocked != user.IsBlocked)
        {
            user.IsBlocked = request.IsBlocked;
            if (request.IsBlocked)
            {
                user.BlockReason = string.IsNullOrWhiteSpace(request.BlockReason)
                    ? "Bloqueado por administrador"
                    : request.BlockReason.Trim();
                user.BlockedAt = DateTime.UtcNow;
                user.BlockedByUserId = actorId;
            }
            else
            {
                user.BlockReason = null;
                user.BlockedAt = null;
                user.BlockedByUserId = null;
            }
        }
        else if (user.IsBlocked)
        {
            user.BlockReason = string.IsNullOrWhiteSpace(request.BlockReason)
                ? user.BlockReason
                : request.BlockReason.Trim();
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }

        var roles = currentRoles;
        await RegisterAuditAsync(actorId, "UserProfileUpdated", "ApplicationUser", id, $"{request.FirstName} {request.LastName}", context);

        if (previousActive != user.IsActive)
        {
            var title = user.IsActive ? "Tu cuenta fue activada" : "Tu cuenta fue desactivada";
            var message = user.IsActive
                ? "Un administrador reactivó tu acceso a ConfidentialBox."
                : "Tu cuenta ha sido desactivada temporalmente. Comunícate con soporte si necesitas más información.";
            var severity = user.IsActive ? "success" : "warning";
            await _userNotificationService.CreateAsync(user.Id, title, message, severity, null, actorId);

            if (!user.IsActive)
            {
                await _userMessageService.CreateAsync(user.Id, "Cuenta desactivada", "Se desactivó tu cuenta por decisión administrativa. Responde este mensaje si necesitas asistencia.", actorId, requiresResponse: true);
            }
        }

        if (!previousBlocked && user.IsBlocked)
        {
            var reason = user.BlockReason ?? "Bloqueo administrativo";
            await _userNotificationService.CreateAsync(user.Id, "Tu cuenta fue bloqueada", reason, "danger", null, actorId);
            await _userMessageService.CreateAsync(user.Id, "Cuenta bloqueada", $"Tu acceso fue bloqueado. Motivo: {reason}", actorId, requiresResponse: true);
        }
        else if (previousBlocked && !user.IsBlocked)
        {
            await _userNotificationService.CreateAsync(user.Id, "Cuenta desbloqueada", "Tu acceso fue restablecido. Puedes ingresar nuevamente.", "success", null, actorId);
            await _userMessageService.CreateAsync(user.Id, "Cuenta desbloqueada", "Tu acceso fue restablecido por un administrador.", actorId);
        }
        else if (user.IsBlocked && !string.Equals(previousBlockReason, user.BlockReason, StringComparison.OrdinalIgnoreCase))
        {
            var reason = user.BlockReason ?? "Bloqueo administrativo";
            await _userNotificationService.CreateAsync(user.Id, "Motivo de bloqueo actualizado", reason, "warning", null, actorId);
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            IsBlocked = user.IsBlocked,
            BlockReason = user.BlockReason,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList()
        });
    }

    [HttpGet("{id}/audit")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<AuditLogDto>>> GetUserAudit(string id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var logs = await _auditLogRepository.GetByUserIdAsync(id, pageNumber, pageSize);
        var dtos = logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            UserName = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : l.UserId,
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            Timestamp = l.Timestamp,
            IpAddress = l.IpAddress,
            DeviceName = l.DeviceName,
            DeviceType = l.DeviceType,
            OperatingSystem = l.OperatingSystem,
            Browser = l.Browser,
            Location = l.Location,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            UserAgent = l.UserAgent
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            IsBlocked = user.IsBlocked,
            BlockReason = user.BlockReason,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList()
        });
    }

    [HttpGet("me/messages")]
    [Authorize]
    public async Task<ActionResult<List<UserMessageDto>>> GetMyMessages()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var messages = await _userMessageService.GetRecentAsync(userId, 50);
        var dtos = messages.Select(m => new UserMessageDto
        {
            Id = m.Id,
            Subject = m.Subject,
            Body = m.Body,
            CreatedAt = m.CreatedAt,
            IsRead = m.IsRead,
            SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : null,
            RequiresResponse = m.RequiresResponse
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("me/messages/{messageId}/read")]
    [Authorize]
    public async Task<ActionResult> MarkMyMessageAsRead(int messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _userMessageService.MarkAsReadAsync(userId, messageId);
        return NoContent();
    }

    [HttpPost("me/messages/{messageId}/reply")]
    [Authorize]
    public async Task<ActionResult<OperationResultDto>> ReplyToMessage(int messageId, [FromBody] UserMessageReplyRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = "Debes escribir un mensaje para responder."
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new OperationResultDto { Success = false, Error = "Sesión inválida" });
        }

        var message = await _userMessageService.GetByIdAsync(messageId);
        if (message == null || !string.Equals(message.UserId, userId, StringComparison.Ordinal))
        {
            return NotFound(new OperationResultDto { Success = false, Error = "Mensaje no encontrado" });
        }

        var recipients = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(message.SenderId))
        {
            recipients.Add(message.SenderId);
        }

        if (recipients.Count == 0)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                if (!string.Equals(admin.Id, userId, StringComparison.Ordinal))
                {
                    recipients.Add(admin.Id);
                }
            }
        }

        var sender = await _userManager.FindByIdAsync(userId);
        var senderName = sender != null
            ? $"{sender.FirstName} {sender.LastName}".Trim()
            : "Usuario";
        var subject = string.IsNullOrWhiteSpace(message.Subject)
            ? "Respuesta del usuario"
            : $"Re: {message.Subject}";

        var replyBody = request.Body.Trim();

            foreach (var recipient in recipients)
            {
                await _userMessageService.CreateAsync(recipient, subject, replyBody, userId);
                await _userNotificationService.CreateAsync(
                    recipient,
                    "Nueva respuesta recibida",
                    $"{senderName} respondió: {subject}",
                    "info",
                    "/profile?tab=messages",
                    userId);
            }

        message.RequiresResponse = false;
        message.IsRead = true;
        await _userMessageService.UpdateAsync(message);

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpPost("{id}/messages")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OperationResultDto>> SendMessageToUser(string id, [FromBody] CreateUserMessageRequest request)
    {
        if (request == null)
        {
            return BadRequest(new OperationResultDto { Success = false, Error = "Debes especificar el contenido del mensaje." });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new OperationResultDto { Success = false, Error = "Usuario no encontrado" });
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? "Mensaje del administrador"
            : request.Subject.Trim();
        var body = string.IsNullOrWhiteSpace(request.Body)
            ? "Tienes un nuevo mensaje del equipo administrador."
            : request.Body.Trim();

        await _userMessageService.CreateAsync(user.Id, subject, body, actorId, request.RequiresResponse);
        await _userNotificationService.CreateAsync(
            user.Id,
            "Nuevo mensaje del administrador",
            subject,
            "info",
            "/profile?tab=messages",
            actorId);

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpPut("me/profile")]
    public async Task<ActionResult<OperationResultDto>> UpdateMyProfile([FromBody] SelfProfileUpdateRequest request)
    {
        if (request == null)
        {
            return BadRequest(new OperationResultDto { Success = false, Error = "Los datos del perfil son obligatorios" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new OperationResultDto { Success = false, Error = "Sesión inválida" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new OperationResultDto { Success = false, Error = "Sesión inválida" });
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalized = request.Email.Trim();
            if (!string.Equals(user.Email, normalized, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(normalized);
                if (existing != null && existing.Id != user.Id)
                {
                    return BadRequest(new OperationResultDto { Success = false, Error = "El email ya está en uso." });
                }

                user.Email = normalized;
                user.UserName = normalized;
            }
        }

        await _userManager.UpdateAsync(user);

        var context = _clientContextResolver.Resolve(HttpContext);
        await RegisterAuditAsync(userId, "SelfProfileUpdated", "ApplicationUser", userId, null, context);

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpPut("me/password")]
    public async Task<ActionResult<OperationResultDto>> ChangeMyPassword([FromBody] ChangeOwnPasswordRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NewPassword) || string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return BadRequest(new OperationResultDto { Success = false, Error = "Debes ingresar la contraseña actual y la nueva contraseña" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new OperationResultDto { Success = false, Error = "Sesión inválida" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new OperationResultDto { Success = false, Error = "Sesión inválida" });
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var detail = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new OperationResultDto { Success = false, Detail = detail });
        }

        var context = _clientContextResolver.Resolve(HttpContext);
        await RegisterAuditAsync(userId, "SelfPasswordChanged", "ApplicationUser", userId, null, context);
        return Ok(new OperationResultDto { Success = true });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public ActionResult<OperationResultDto> Delete(string id)
    {
        return BadRequest(new OperationResultDto
        {
            Success = false,
            Error = "La eliminación de usuarios está deshabilitada. Desactiva el usuario en su lugar."
        });
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
}