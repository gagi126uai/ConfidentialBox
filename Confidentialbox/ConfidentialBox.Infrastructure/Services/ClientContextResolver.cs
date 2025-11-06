using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace ConfidentialBox.Infrastructure.Services;

public class ClientContextResolver : IClientContextResolver
{
    private static readonly Regex WindowsRegex = new("Windows NT (?<version>[0-9.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MacRegex = new("Mac OS X (?<version>[0-9_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AndroidRegex = new("Android (?<version>[0-9.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IOSRegex = new("(iPhone|iPad); CPU (?<os>iPhone)? OS (?<version>[0-9_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ClientContext Resolve(HttpContext context)
    {
        var headers = context.Request.Headers;
        var ip = headers.TryGetValue("X-Forwarded-For", out var forwarded) && forwarded.Count > 0
            ? forwarded[0].Split(',')[0].Trim()
            : context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var userAgent = headers["User-Agent"].ToString();
        var deviceName = headers["X-Device-Name"].ToString();

        var (deviceType, os) = ParseOperatingSystem(userAgent);
        var browser = ParseBrowser(userAgent);

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

        var deviceTypeName = DetermineDeviceType(userAgent, deviceType);

        return new ClientContext(
            ip,
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            string.IsNullOrWhiteSpace(deviceName) ? null : deviceName,
            deviceTypeName,
            os,
            browser,
            location,
            latitude,
            longitude);
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
}
