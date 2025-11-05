namespace ConfidentialBox.Core.DTOs;

public class OperationResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Detail { get; set; }
}
