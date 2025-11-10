namespace ConfidentialBox.Core.DTOs;

public class ChangeUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
