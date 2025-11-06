namespace ConfidentialBox.Core.DTOs;

public class UpdateRolePoliciesRequest
{
    public Dictionary<string, string> Policies { get; set; } = new();
}
