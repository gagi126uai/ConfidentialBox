namespace ConfidentialBox.Core.DTOs;

public class ReviewAlertRequest
{
    public int AlertId { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
    public string ActionTaken { get; set; } = string.Empty;
}