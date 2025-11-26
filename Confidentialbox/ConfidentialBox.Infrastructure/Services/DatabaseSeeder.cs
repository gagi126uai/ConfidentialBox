using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ConfidentialBox.Infrastructure.Services;

public class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Crear roles
        var adminRole = await CreateRoleIfNotExists(roleManager, "Admin", "Administrador del sistema", true);
        var userRole = await CreateRoleIfNotExists(roleManager, "User", "Usuario estándar", true);
        var guestRole = await CreateRoleIfNotExists(roleManager, "Guest", "Usuario invitado", true);
        var auditorRole = await CreateRoleIfNotExists(roleManager, "Auditor", "Auditor operativo", true);

        await EnsureRolePoliciesAsync(roleManager, adminRole);
        await EnsureRolePoliciesAsync(roleManager, userRole);
        await EnsureRolePoliciesAsync(roleManager, guestRole);
        await EnsureRolePoliciesAsync(roleManager, auditorRole);

        // Crear usuario administrador predeterminado
        await CreateUserIfNotExists(
            userManager,
            "admin",
            "admin@confidentialbox.local",
            "admin",
            "Admin",
            "Principal",
            new[] { "Admin" });

        // Crear usuario estándar de ejemplo
        await CreateUserIfNotExists(
            userManager,
            "user",
            "user@confidentialbox.com",
            "User123!",
            "John",
            "Doe",
            new[] { "User" });

        await CreateUserIfNotExists(
            userManager,
            "auditor",
            "auditor@confidentialbox.com",
            "Auditor123!",
            "Auditor",
            "Operaciones",
            new[] { "Auditor" });
    }

    private static async Task<ApplicationRole> CreateRoleIfNotExists(RoleManager<ApplicationRole> roleManager, string roleName, string description, bool isSystemRole)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new ApplicationRole
            {
                Name = roleName,
                Description = description,
                IsSystemRole = isSystemRole,
                CreatedAt = DateTime.UtcNow
            };
            await roleManager.CreateAsync(role);
        }
        return role;
    }

    private static async Task EnsureRolePoliciesAsync(RoleManager<ApplicationRole> roleManager, ApplicationRole role)
    {
        var existingRole = await roleManager.Roles
            .Where(r => r.Id == role.Id)
            .Select(r => new
            {
                Role = r,
                Policies = r.RolePolicies
            })
            .FirstOrDefaultAsync();

        if (existingRole == null)
        {
            return;
        }

        var currentPolicies = existingRole.Policies.ToDictionary(p => p.PolicyName, p => p, StringComparer.OrdinalIgnoreCase);
        var defaults = RolePolicyCatalog.GetDefaultValuesForRole(role.Name ?? string.Empty);

        foreach (var definition in RolePolicyCatalog.Definitions)
        {
            if (currentPolicies.ContainsKey(definition.Key))
            {
                continue;
            }

            var value = defaults.TryGetValue(definition.Key, out var defaultValue)
                ? defaultValue
                : definition.DefaultValue ?? string.Empty;

            role.RolePolicies.Add(new RolePolicy
            {
                PolicyName = definition.Key,
                PolicyValue = value
            });
        }

        await roleManager.UpdateAsync(role);
    }

    private static async Task CreateUserIfNotExists(
        UserManager<ApplicationUser> userManager,
        string userName,
        string email,
        string password,
        string firstName,
        string lastName,
        string[] roles)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user == null)
        {
            user = await userManager.FindByEmailAsync(email);
        }

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(user, roles);
            }
        }
        else
        {
            // Asegurar que el usuario tenga los roles esperados
            var existingRoles = await userManager.GetRolesAsync(user);
            var missingRoles = roles.Except(existingRoles);
            if (missingRoles.Any())
            {
                await userManager.AddToRolesAsync(user, missingRoles);
            }
        }
    }
}
