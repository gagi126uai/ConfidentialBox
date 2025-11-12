using System.Collections.Generic;

namespace ConfidentialBox.Core.DTOs;

public class ReviewAlertRequest
{
    public int AlertId { get; set; }
    public string Status { get; set; } = "Pending";
    public string ReviewNotes { get; set; } = string.Empty;
    public string? Verdict { get; set; }
    public string ActionTaken { get; set; } = string.Empty;
    public List<AlertActionCommand> Actions { get; set; } = new();
}
