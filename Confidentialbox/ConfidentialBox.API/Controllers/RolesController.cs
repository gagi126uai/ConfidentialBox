using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConfidentialBox.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RolesController(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetAll()
    {
        var roles = await _roleManager.Roles
            .Include(r => r.RolePolicies)
            .ToListAsync();

        var roleDtos = roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name!,
            Description = r.Description,
            IsSystemRole = r.IsSystemRole,
            CreatedAt = r.CreatedAt,
            Policies = r.RolePolicies.Select(p => new PolicyDto
            {
                Id = p.Id,
                PolicyName = p.PolicyName,
                PolicyValue = p.PolicyValue
            }).ToList()
        }).ToList();

        return Ok(roleDtos);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequest request)
    {
        var existingRole = await _roleManager.FindByNameAsync(request.Name);
        if (existingRole != null)
        {
            return BadRequest("Ya existe un rol con ese nombre");
        }

        var role = new ApplicationRole
        {
            Name = request.Name,
            Description = request.Description,
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // Aquí agregarías las políticas si se especificaron

        return Ok(new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedAt,
            Policies = new List<PolicyDto>()
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        if (role.IsSystemRole)
        {
            return BadRequest("No se pueden eliminar roles del sistema");
        }

        await _roleManager.DeleteAsync(role);
        return Ok();
    }
}