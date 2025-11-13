namespace ConfidentialBox.Core.DTOs;

public class CreateUserMessageRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool RequiresResponse { get; set; }
}
