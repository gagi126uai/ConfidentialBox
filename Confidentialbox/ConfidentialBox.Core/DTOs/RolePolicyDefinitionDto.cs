namespace ConfidentialBox.Core.DTOs;

public class RolePolicyDefinitionDto
{
    public string PolicyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public IReadOnlyList<string> Options { get; set; } = Array.Empty<string>();
}
