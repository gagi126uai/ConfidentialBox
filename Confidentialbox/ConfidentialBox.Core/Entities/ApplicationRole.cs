using Microsoft.AspNetCore.Identity;

namespace ConfidentialBox.Core.Entities;

public class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSystemRole { get; set; } = false;

    public virtual ICollection<RolePolicy> RolePolicies { get; set; } = new List<RolePolicy>();
    public virtual ICollection<FilePermission> FilePermissions { get; set; } = new List<FilePermission>();
}