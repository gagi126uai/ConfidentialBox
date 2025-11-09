using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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
    public async Task<ActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        return Ok();
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

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<OperationResultDto>> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new OperationResultDto
            {
                Success = false,
                Error = "Usuario no encontrado"
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = "Los usuarios administradores no pueden ser eliminados."
            });
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "No fue posible eliminar el usuario.";
            return BadRequest(new OperationResultDto
            {
                Success = false,
                Error = error
            });
        }

        return Ok(new OperationResultDto { Success = true });
    }
}