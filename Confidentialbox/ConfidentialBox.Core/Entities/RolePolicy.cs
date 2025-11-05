namespace ConfidentialBox.Core.Entities;

public class RolePolicy
{
    public int Id { get; set; }

    public string RoleId { get; set; } = string.Empty;
    public virtual ApplicationRole Role { get; set; } = null!;

    public string PolicyName { get; set; } = string.Empty;

    public string PolicyValue { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}