namespace ConfidentialBox.Core.DTOs;

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
}
