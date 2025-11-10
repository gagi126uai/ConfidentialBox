namespace ConfidentialBox.Core.DTOs;

public class ChangeOwnPasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
