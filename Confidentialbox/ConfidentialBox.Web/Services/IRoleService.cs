using ConfidentialBox.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public interface IRoleService
{
    Task<List<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> CreateRoleAsync(CreateRoleRequest request);
    Task<List<RolePolicyDefinitionDto>> GetPolicyDefinitionsAsync();
    Task<RoleDto?> UpdatePoliciesAsync(string roleId, Dictionary<string, string> policies);
    Task<bool> DeleteRoleAsync(string id);
}