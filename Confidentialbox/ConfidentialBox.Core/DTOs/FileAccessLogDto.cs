namespace ConfidentialBox.Core.DTOs;

public class FileAccessLogDto
{
    public int Id { get; set; }
    public DateTime AccessedAt { get; set; }
    public bool WasAuthorized { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? AccessedByUserName { get; set; }
    public string AccessedByIp { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Browser { get; set; }
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? UserAgent { get; set; }
}
