using ConfidentialBox.Web.Models;

namespace ConfidentialBox.Web.Services;

public class ClientContextMessageHandler : DelegatingHandler
{
    private readonly ClientContextService _clientContextService;

    public ClientContextMessageHandler(ClientContextService clientContextService)
    {
        _clientContextService = clientContextService;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _clientContextService.Current;
        if (context != null)
        {
            if (!string.IsNullOrWhiteSpace(context.IpAddress))
            {
                request.Headers.Remove("X-Client-IP");
                request.Headers.TryAddWithoutValidation("X-Client-IP", context.IpAddress);
                request.Headers.Remove("X-Forwarded-For");
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", context.IpAddress);
            }

            if (!string.IsNullOrWhiteSpace(context.UserAgent))
            {
                request.Headers.Remove("X-Client-UserAgent");
                request.Headers.TryAddWithoutValidation("X-Client-UserAgent", context.UserAgent);
                request.Headers.Remove("User-Agent");
                request.Headers.TryAddWithoutValidation("User-Agent", context.UserAgent);
            }

            if (!string.IsNullOrWhiteSpace(context.DeviceName))
            {
                request.Headers.Remove("X-Device-Name");
                request.Headers.TryAddWithoutValidation("X-Device-Name", context.DeviceName);
            }

            if (!string.IsNullOrWhiteSpace(context.DeviceType))
            {
                request.Headers.Remove("X-Device-Type");
                request.Headers.TryAddWithoutValidation("X-Device-Type", context.DeviceType);
            }

            if (!string.IsNullOrWhiteSpace(context.OperatingSystem))
            {
                request.Headers.Remove("X-Device-OS");
                request.Headers.TryAddWithoutValidation("X-Device-OS", context.OperatingSystem);
            }

            if (!string.IsNullOrWhiteSpace(context.Browser))
            {
                request.Headers.Remove("X-Device-Browser");
                request.Headers.TryAddWithoutValidation("X-Device-Browser", context.Browser);
            }

            if (!string.IsNullOrWhiteSpace(context.Location))
            {
                request.Headers.Remove("X-Geo-Location");
                request.Headers.TryAddWithoutValidation("X-Geo-Location", context.Location);
            }

            if (context.Latitude.HasValue)
            {
                request.Headers.Remove("X-Geo-Lat");
                request.Headers.TryAddWithoutValidation("X-Geo-Lat", context.Latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (context.Longitude.HasValue)
            {
                request.Headers.Remove("X-Geo-Lon");
                request.Headers.TryAddWithoutValidation("X-Geo-Lon", context.Longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(context.TimeZone))
            {
                request.Headers.Remove("X-Client-TimeZone");
                request.Headers.TryAddWithoutValidation("X-Client-TimeZone", context.TimeZone);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
