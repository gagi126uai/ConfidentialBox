using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
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
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
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
}