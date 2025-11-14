using System;
using ConfidentialBox.Core.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace ConfidentialBox.Infrastructure.Services;

public sealed class SecurePdfSessionPolicyStore : ISecurePdfSessionPolicyStore
{
    private readonly IMemoryCache _cache;

    public SecurePdfSessionPolicyStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Store(string sessionId, PDFViewerSettingsDto settings, int maxViewMinutes)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        var snapshot = CloneSettings(settings);
        var policy = new SecurePdfSessionPolicy(snapshot, maxViewMinutes, DateTimeOffset.UtcNow);
        var options = new MemoryCacheEntryOptions();

        if (maxViewMinutes > 0)
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Clamp(maxViewMinutes + 5, maxViewMinutes, maxViewMinutes + 60));
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
        }

        _cache.Set(sessionId, policy, options);
    }

    public bool TryGet(string sessionId, out SecurePdfSessionPolicy? policy)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            policy = null;
            return false;
        }

        if (_cache.TryGetValue(sessionId, out SecurePdfSessionPolicy cached))
        {
            policy = cached;
            return true;
        }

        policy = null;
        return false;
    }

    public void Remove(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        _cache.Remove(sessionId);
    }

    private static PDFViewerSettingsDto CloneSettings(PDFViewerSettingsDto settings)
    {
        if (settings == null)
        {
            return new PDFViewerSettingsDto();
        }

        return new PDFViewerSettingsDto
        {
            Theme = settings.Theme,
            AccentColor = settings.AccentColor,
            BackgroundColor = settings.BackgroundColor,
            ToolbarBackgroundColor = settings.ToolbarBackgroundColor,
            ToolbarTextColor = settings.ToolbarTextColor,
            FontFamily = settings.FontFamily,
            ShowToolbar = settings.ShowToolbar,
            ToolbarPosition = settings.ToolbarPosition,
            ShowFileDetails = settings.ShowFileDetails,
            ShowSearch = settings.ShowSearch,
            ShowPageControls = settings.ShowPageControls,
            ShowPageIndicator = settings.ShowPageIndicator,
            ShowDownloadButton = settings.ShowDownloadButton,
            ShowPrintButton = settings.ShowPrintButton,
            ShowFullscreenButton = settings.ShowFullscreenButton,
            AllowDownload = settings.AllowDownload,
            AllowPrint = settings.AllowPrint,
            AllowCopy = settings.AllowCopy,
            DisableTextSelection = settings.DisableTextSelection,
            DisableContextMenu = settings.DisableContextMenu,
            ForceGlobalWatermark = settings.ForceGlobalWatermark,
            GlobalWatermarkText = settings.GlobalWatermarkText,
            WatermarkOpacity = settings.WatermarkOpacity,
            WatermarkFontSize = settings.WatermarkFontSize,
            WatermarkColor = settings.WatermarkColor,
            MaxViewTimeMinutes = settings.MaxViewTimeMinutes,
            DefaultZoomPercent = settings.DefaultZoomPercent,
            ZoomStepPercent = settings.ZoomStepPercent,
            ViewerPadding = settings.ViewerPadding,
            CustomCss = settings.CustomCss
        };
    }
}
