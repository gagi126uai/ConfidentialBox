using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace ConfidentialBox.Infrastructure.Services;

public class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Crear roles
        await CreateRoleIfNotExists(roleManager, "Admin", "Administrador del sistema", true);
        await CreateRoleIfNotExists(roleManager, "User", "Usuario estándar", true);
        await CreateRoleIfNotExists(roleManager, "Guest", "Usuario invitado", true);

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
    }

    private static async Task CreateRoleIfNotExists(RoleManager<ApplicationRole> roleManager, string roleName, string description, bool isSystemRole)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = new ApplicationRole
            {
                Name = roleName,
                Description = description,
                IsSystemRole = isSystemRole,
                CreatedAt = DateTime.UtcNow
            };
            await roleManager.CreateAsync(role);
        }
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
