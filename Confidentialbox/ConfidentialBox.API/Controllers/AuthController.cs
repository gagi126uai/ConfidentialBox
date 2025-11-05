using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ISystemSettingsService _systemSettingsService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IEmailNotificationService emailNotificationService,
        ISystemSettingsService systemSettingsService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _emailNotificationService = emailNotificationService;
        _systemSettingsService = systemSettingsService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.Identifier?.Trim() ?? string.Empty;
        ApplicationUser? user = null;

        if (!string.IsNullOrWhiteSpace(identifier) && identifier.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(identifier);
        }

        if (user == null && !string.IsNullOrWhiteSpace(identifier))
        {
            user = await _userManager.FindByNameAsync(identifier);
        }

        if (user == null && !string.IsNullOrWhiteSpace(identifier))
        {
            user = await _userManager.Users.FirstOrDefaultAsync(
                u => u.Email == identifier,
                cancellationToken);
        }

        if (user == null || !user.IsActive)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                ErrorMessage = "Credenciales inválidas o usuario inactivo"
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                ErrorMessage = "Credenciales inválidas"
            });
        }

        // Actualizar último login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generar JWT token
        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles.ToList());

        return Ok(new LoginResponse
        {
            Success = true,
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles.ToList()
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!await _systemSettingsService.IsUserRegistrationEnabledAsync(cancellationToken))
        {
            return Ok(new LoginResponse
            {
                Success = false,
                ErrorMessage = "El registro de usuarios está deshabilitado por el administrador"
            });
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                ErrorMessage = "El email ya está registrado"
            });
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
            return Ok(new LoginResponse
            {
                Success = false,
                ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        // Asignar rol User por defecto
        await _userManager.AddToRoleAsync(user, "User");

        // Generar token
        var roles = new List<string> { "User" };
        var token = GenerateJwtToken(user, roles);

        return Ok(new LoginResponse
        {
            Success = true,
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles
        });
    }

    [HttpGet("registration-status")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationSettingsDto>> GetRegistrationStatus(CancellationToken cancellationToken)
    {
        var enabled = await _systemSettingsService.IsUserRegistrationEnabledAsync(cancellationToken);
        return Ok(new RegistrationSettingsDto { IsRegistrationEnabled = enabled });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return Ok(new OperationResultDto { Success = true });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = _configuration["ClientApp:BaseUrl"] ?? "https://localhost:5001";
        var resetUrl = BuildResetUrl(baseUrl, user.Email!, encodedToken);

        try
        {
            await _emailNotificationService.SendPasswordResetAsync(user, resetUrl, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new OperationResultDto
            {
                Success = false,
                Error = "No fue posible enviar el correo de recuperación. Verifique la configuración del servidor de correo.",
                Detail = ex.Message
            });
        }

        return Ok(new OperationResultDto { Success = true });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Ok(new OperationResultDto { Success = true });
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch
        {
            return BadRequest(new OperationResultDto { Success = false, Error = "Token inválido" });
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Detail = string.Join("; ", resetResult.Errors.Select(e => e.Description))
            });
        }

        return Ok(new OperationResultDto { Success = true });
    }

    private string GenerateJwtToken(ApplicationUser user, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Email!),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim("FullName", $"{user.FirstName} {user.LastName}")
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "SuperSecretKeyForConfidentialBox2024WithMinimum32Characters!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ConfidentialBoxAPI",
            audience: _configuration["Jwt:Audience"] ?? "ConfidentialBoxClient",
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string BuildResetUrl(string baseUrl, string email, string token)
    {
        var normalizedBase = baseUrl.TrimEnd('/');
        var emailParam = Uri.EscapeDataString(email);
        var tokenParam = Uri.EscapeDataString(token);
        return $"{normalizedBase}/reset-password?email={emailParam}&token={tokenParam}";
    }
}