using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

        var definitionLookup = RolePolicyCatalog.Definitions
            .ToDictionary(d => d.Key, d => d, StringComparer.OrdinalIgnoreCase);

        var roleDtos = roles
            .Select(r => MapToDto(r, definitionLookup))
            .ToList();

        return Ok(roleDtos);
    }

    [HttpGet("policy-definitions")]
    public ActionResult<List<RolePolicyDefinitionDto>> GetPolicyDefinitions()
    {
        var result = RolePolicyCatalog.Definitions
            .Select(d => new RolePolicyDefinitionDto
            {
                PolicyName = d.Key,
                DisplayName = d.DisplayName,
                Description = d.Description,
                ValueType = d.ValueType.ToString(),
                DefaultValue = d.DefaultValue,
                Options = d.Options
            })
            .ToList();

        return Ok(result);
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

        role = await _roleManager.Roles
            .Include(r => r.RolePolicies)
            .FirstAsync(r => r.Id == role.Id);

        await EnsureRolePoliciesAsync(role);

        var dto = MapToDto(role, RolePolicyCatalog.Definitions.ToDictionary(d => d.Key, d => d, StringComparer.OrdinalIgnoreCase));
        return Ok(dto);
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

    [HttpPut("{id}/policies")]
    public async Task<ActionResult<RoleDto>> UpdatePolicies(string id, [FromBody] UpdateRolePoliciesRequest request)
    {
        if (request == null)
        {
            return BadRequest("Solicitud inválida");
        }

        var role = await _roleManager.Roles
            .Include(r => r.RolePolicies)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return NotFound();
        }

        var definitions = RolePolicyCatalog.Definitions.ToDictionary(d => d.Key, d => d, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in request.Policies)
        {
            if (!definitions.TryGetValue(entry.Key, out var definition))
            {
                return BadRequest($"Política desconocida: {entry.Key}");
            }

            string normalized;
            try
            {
                normalized = NormalizePolicyValue(entry.Value, definition);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            var existing = role.RolePolicies.FirstOrDefault(p => p.PolicyName.Equals(definition.Key, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                existing = new RolePolicy
                {
                    PolicyName = definition.Key,
                    PolicyValue = normalized
                };
                role.RolePolicies.Add(existing);
            }
            else
            {
                existing.PolicyValue = normalized;
            }
        }

        await _roleManager.UpdateAsync(role);

        var dto = MapToDto(role, definitions);
        return Ok(dto);
    }

    private static RoleDto MapToDto(ApplicationRole role, IDictionary<string, RolePolicyDefinition> definitions)
    {
        var policies = new List<PolicyDto>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions.Values)
        {
            var existing = role.RolePolicies.FirstOrDefault(p => p.PolicyName.Equals(definition.Key, StringComparison.OrdinalIgnoreCase));
            policies.Add(new PolicyDto
            {
                Id = existing?.Id ?? 0,
                PolicyName = definition.Key,
                PolicyValue = existing?.PolicyValue ?? definition.DefaultValue ?? string.Empty,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                ValueType = definition.ValueType.ToString(),
                Options = definition.Options
            });
            usedKeys.Add(definition.Key);
        }

        foreach (var extra in role.RolePolicies.Where(p => !usedKeys.Contains(p.PolicyName)))
        {
            policies.Add(new PolicyDto
            {
                Id = extra.Id,
                PolicyName = extra.PolicyName,
                PolicyValue = extra.PolicyValue,
                DisplayName = extra.PolicyName,
                Description = string.Empty,
                ValueType = PolicyValueType.Text.ToString(),
                Options = Array.Empty<string>()
            });
        }

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedAt,
            Policies = policies
        };
    }

    private async Task EnsureRolePoliciesAsync(ApplicationRole role)
    {
        var definitions = RolePolicyCatalog.GetDefaultValuesForRole(role.Name ?? string.Empty);

        foreach (var definition in RolePolicyCatalog.Definitions)
        {
            var hasPolicy = role.RolePolicies.Any(p => p.PolicyName.Equals(definition.Key, StringComparison.OrdinalIgnoreCase));
            if (hasPolicy)
            {
                continue;
            }

            var value = definitions.TryGetValue(definition.Key, out var defaultValue)
                ? defaultValue
                : definition.DefaultValue ?? string.Empty;

            role.RolePolicies.Add(new RolePolicy
            {
                PolicyName = definition.Key,
                PolicyValue = value
            });
        }

        await _roleManager.UpdateAsync(role);
    }

    private static string NormalizePolicyValue(string? rawValue, RolePolicyDefinition definition)
    {
        rawValue ??= definition.DefaultValue ?? string.Empty;

        return definition.ValueType switch
        {
            PolicyValueType.Boolean => bool.TryParse(rawValue, out var boolValue)
                ? boolValue.ToString().ToLowerInvariant()
                : throw new ArgumentException($"El valor de {definition.DisplayName} debe ser verdadero o falso"),
            PolicyValueType.Number => decimal.TryParse(rawValue, out var number)
                ? number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : throw new ArgumentException($"El valor de {definition.DisplayName} debe ser numérico"),
            _ => rawValue
        };
    }
}