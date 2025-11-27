using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using UAParser;

namespace ConfidentialBox.Infrastructure.Services;

public class ClientContextResolver : IClientContextResolver
{
    private static readonly Parser UserAgentParser = Parser.GetDefault();
    private static readonly Regex WindowsRegex = new("Windows NT (?<version>[0-9.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MacRegex = new("Mac OS X (?<version>[0-9_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AndroidRegex = new("Android (?<version>[0-9.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IOSRegex = new("(iPhone|iPad); CPU (?<os>iPhone)? OS (?<version>[0-9_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ClientContext Resolve(HttpContext context)
    {
        var headers = context.Request.Headers;
        var ipHeader = headers.TryGetValue("X-Client-IP", out var clientIp) ? clientIp.FirstOrDefault() : null;
        var forwardedHeader = headers.TryGetValue("X-Forwarded-For", out var forwarded) ? forwarded.FirstOrDefault() : null;
        var ip = !string.IsNullOrWhiteSpace(ipHeader)
            ? ipHeader
            : !string.IsNullOrWhiteSpace(forwardedHeader)
                ? forwardedHeader.Split(',')[0].Trim()
                : context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var userAgent = headers.TryGetValue("X-Client-UserAgent", out var forwardedUa) && !string.IsNullOrWhiteSpace(forwardedUa)
            ? forwardedUa.ToString()
            : headers["User-Agent"].ToString();
        var deviceName = headers["X-Device-Name"].ToString();
        if (string.IsNullOrWhiteSpace(deviceName) && headers.TryGetValue("X-Device-Name", out var deviceNameHeader))
        {
            deviceName = deviceNameHeader.ToString();
        }

        var explicitDeviceType = headers.TryGetValue("X-Device-Type", out var deviceTypeHeader) ? deviceTypeHeader.ToString() : null;
        var explicitOs = headers.TryGetValue("X-Device-OS", out var osHeader) ? osHeader.ToString() : null;
        var explicitBrowser = headers.TryGetValue("X-Device-Browser", out var browserHeader) ? browserHeader.ToString() : null;

        var parsedUa = ParseUserAgent(userAgent);
        var (regexDeviceType, regexOs) = ParseOperatingSystem(userAgent);
        var operatingSystem = !string.IsNullOrWhiteSpace(explicitOs)
            ? explicitOs
            : parsedUa.OperatingSystem ?? regexOs;
        var browser = !string.IsNullOrWhiteSpace(explicitBrowser)
            ? explicitBrowser
            : parsedUa.Browser ?? ParseBrowser(userAgent);

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            deviceName = parsedUa.DeviceFamily;
        }

        var locationHeader = headers["X-Geo-Location"].ToString();
        string? location = null;
        double? latitude = null;
        double? longitude = null;
        if (!string.IsNullOrWhiteSpace(locationHeader))
        {
            location = locationHeader;
        }

        if (headers.TryGetValue("X-Geo-Lat", out var latValue) && double.TryParse(latValue, out var lat))
        {
            latitude = lat;
        }

        if (headers.TryGetValue("X-Geo-Lon", out var lonValue) && double.TryParse(lonValue, out var lon))
        {
            longitude = lon;
        }

        if (string.IsNullOrEmpty(location) && IsLoopback(ip))
        {
            location = "Red local";
        }

        var timeZone = headers.TryGetValue("X-Client-TimeZone", out var timeZoneHeader)
            ? timeZoneHeader.ToString()
            : null;
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            timeZone = null;
        }

        var deviceTypeName = !string.IsNullOrWhiteSpace(explicitDeviceType)
            ? explicitDeviceType
            : DetermineDeviceType(userAgent, parsedUa.DeviceType ?? regexDeviceType);

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            deviceName = deviceTypeName;
        }

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            deviceName = "Dispositivo desconocido";
        }

        return new ClientContext(
            ip,
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            string.IsNullOrWhiteSpace(deviceName) ? null : deviceName,
            deviceTypeName,
            operatingSystem,
            browser,
            location,
            latitude,
            longitude,
            timeZone);
    }

    private static bool IsLoopback(string ip)
    {
        if (IPAddress.TryParse(ip, out var address))
        {
            return IPAddress.IsLoopback(address);
        }
        return false;
    }

    private static (string deviceType, string? os) ParseOperatingSystem(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return ("Desconocido", null);
        }

        var windowsMatch = WindowsRegex.Match(userAgent);
        if (windowsMatch.Success)
        {
            return ("Desktop", $"Windows {windowsMatch.Groups["version"].Value}");
        }

        var macMatch = MacRegex.Match(userAgent);
        if (macMatch.Success)
        {
            var version = macMatch.Groups["version"].Value.Replace('_', '.');
            return ("Desktop", $"macOS {version}");
        }

        var androidMatch = AndroidRegex.Match(userAgent);
        if (androidMatch.Success)
        {
            return ("Mobile", $"Android {androidMatch.Groups["version"].Value}");
        }

        var iosMatch = IOSRegex.Match(userAgent);
        if (iosMatch.Success)
        {
            var version = iosMatch.Groups["version"].Value.Replace('_', '.');
            return ("Mobile", $"iOS {version}");
        }

        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            return ("Desktop", "Linux");
        }

        return ("Desconocido", null);
    }

    private static (string? Browser, string? OperatingSystem, string? DeviceType, string? DeviceFamily) ParseUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return (null, null, null, null);
        }

        try
        {
            var clientInfo = UserAgentParser.Parse(userAgent);
            var os = FormatOperatingSystem(clientInfo.OS);
            var browser = FormatBrowser(clientInfo.UA);
            var deviceType = NormalizeDeviceType(clientInfo.Device);
            var deviceFamily = NormalizeDeviceFamily(clientInfo.Device);
            return (browser, os, deviceType, deviceFamily);
        }
        catch
        {
            return (null, null, null, null);
        }
    }

    private static string? ParseBrowser(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            return "Microsoft Edge";
        }
        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
        {
            return "Google Chrome";
        }
        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
        {
            return "Mozilla Firefox";
        }
        if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
        {
            return "Safari";
        }
        if (userAgent.Contains("MSIE", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("Trident", StringComparison.OrdinalIgnoreCase))
        {
            return "Internet Explorer";
        }

        return null;
    }

    private static string? NormalizeDeviceFamily(Device device)
    {
        if (device == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(device.Brand) && !string.Equals(device.Brand, "Generic", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(device.Model) && !string.Equals(device.Model, "Generic", StringComparison.OrdinalIgnoreCase))
            {
                return $"{device.Brand} {device.Model}".Trim();
            }

            return device.Brand;
        }

        if (!string.IsNullOrWhiteSpace(device.Model) && !string.Equals(device.Model, "Generic", StringComparison.OrdinalIgnoreCase))
        {
            return device.Model;
        }

        return null;
    }

    private static string DetermineDeviceType(string userAgent, string osDeviceType)
    {
        if (!string.IsNullOrWhiteSpace(osDeviceType) && !osDeviceType.Equals("Desconocido", StringComparison.OrdinalIgnoreCase))
        {
            return osDeviceType;
        }

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Desconocido";
        }

        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "Mobile";
        }
        if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            return "Tablet";
        }

        return "Desktop";
    }

    private static string? FormatOperatingSystem(OS os)
    {
        if (os == null)
        {
            return null;
        }

        var family = os.Family;
        if (string.IsNullOrWhiteSpace(family))
        {
            return null;
        }

        var versions = new[] { os.Major, os.Minor, os.Patch, os.PatchMinor }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return versions.Length > 0 ? $"{family} {string.Join('.', versions)}" : family;
    }

    private static string? FormatBrowser(UserAgent ua)
    {
        if (ua == null || string.IsNullOrWhiteSpace(ua.Family))
        {
            return null;
        }

        var versions = new[] { ua.Major, ua.Minor, ua.Patch }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return versions.Length > 0 ? $"{ua.Family} {string.Join('.', versions)}" : ua.Family;
    }

    private static string? NormalizeDeviceType(Device device)
    {
        if (device == null || string.IsNullOrWhiteSpace(device.Family) || device.Family.Equals("Other", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var family = device.Family;
        if (family.Contains("tablet", StringComparison.OrdinalIgnoreCase) || family.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            return "Tablet";
        }

        if (family.Contains("mobile", StringComparison.OrdinalIgnoreCase) || family.Contains("phone", StringComparison.OrdinalIgnoreCase) || family.Contains("android", StringComparison.OrdinalIgnoreCase))
        {
            return "Mobile";
        }

        if (family.Contains("spider", StringComparison.OrdinalIgnoreCase) || family.Contains("bot", StringComparison.OrdinalIgnoreCase))
        {
            return "Robot";
        }

        return "Desktop";
    }
}
