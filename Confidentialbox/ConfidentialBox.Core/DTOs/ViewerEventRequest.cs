namespace ConfidentialBox.Core.DTOs;

public class ViewerEventRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? EventData { get; set; }
}