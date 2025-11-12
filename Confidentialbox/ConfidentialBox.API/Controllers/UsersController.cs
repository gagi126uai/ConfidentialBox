using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using ConfidentialBox.Infrastructure.Repositories;
using ConfidentialBox.Infrastructure.Services;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IClientContextResolver _clientContextResolver;

    public UsersController(UserManager<ApplicationUser> userManager, IAuditLogRepository auditLogRepository, IClientContextResolver clientContextResolver)
    {
        _userManager = userManager;
        _auditLogRepository = auditLogRepository;
        _clientContextResolver = clientContextResolver;
    }

    [HttpGet]
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
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles.ToList()
            });
        }

        return Ok(userDtos);
    }

    [HttpGet("{id}")]
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
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList()
        });
    }

    [HttpPost]
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

        if (request.Roles.Any())
        {
            await _userManager.AddToRolesAsync(user, request.Roles);
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        });
    }

    [HttpPut("{id}/toggle-active")]
    public async Task<ActionResult<OperationResultDto>> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new OperationResultDto { Success = false, Error = "Usuario no encontrado" });
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var context = await _clientContextResolver.ResolveAsync(HttpContext);
        await RegisterAuditAsync(actorId, user.IsActive ? "UserActivated" : "UserDeactivated", "ApplicationUser", id, null, context);

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpPut("{id}/roles")]
    public async Task<ActionResult> UpdateRoles(string id, [FromBody] List<string> roles)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRolesAsync(user, roles);

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var context = await _clientContextResolver.ResolveAsync(HttpContext);
        await RegisterAuditAsync(actorId, "UserRolesUpdated", "ApplicationUser", id, string.Join(",", roles), context);

        return Ok();
    }

    [HttpPut("{id}/password")]
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
        var context = await _clientContextResolver.ResolveAsync(HttpContext);
        await RegisterAuditAsync(actorId, "UserPasswordReset", "ApplicationUser", id, request.Reason, context);

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpPut("{id}/profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile(string id, [FromBody] UpdateUserProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email;
            user.UserName = request.Email;
        }

        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var context = await _clientContextResolver.ResolveAsync(HttpContext);
        await RegisterAuditAsync(actorId, "UserProfileUpdated", "ApplicationUser", id, $"{request.FirstName} {request.LastName}", context);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList()
        });
    }

    [HttpGet("{id}/audit")]
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

    [HttpPut("me/profile")]
    public async Task<ActionResult<OperationResultDto>> UpdateMyProfile([FromBody] SelfProfileUpdateRequest request)
    {
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

        await _userManager.UpdateAsync(user);

        var context = await _clientContextResolver.ResolveAsync(HttpContext);
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

        var context = await _clientContextResolver.ResolveAsync(HttpContext);
        await RegisterAuditAsync(userId, "SelfPasswordChanged", "ApplicationUser", userId, null, context);
        return Ok(new OperationResultDto { Success = true });
    }

    [HttpDelete("{id}")]
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
        if (string.IsNullOrWhiteSpace(userId))
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
            IpAddress = context?.IpAddress,
            UserAgent = context?.UserAgent,
            DeviceName = context?.DeviceName,
            DeviceType = context?.DeviceType,
            OperatingSystem = context?.OperatingSystem,
            Browser = context?.Browser,
            Location = context?.Location,
            Latitude = context?.Latitude,
            Longitude = context?.Longitude
        };

        await _auditLogRepository.AddAsync(log);
    }
}
