namespace ConfidentialBox.Infrastructure.Services;

public record ClientContext(
    string IpAddress,
    string? UserAgent,
    string? DeviceName,
    string? DeviceType,
    string? OperatingSystem,
    string? Browser,
    string? Location,
    double? Latitude,
    double? Longitude);
