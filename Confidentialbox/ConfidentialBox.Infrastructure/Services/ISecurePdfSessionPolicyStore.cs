using System;
using ConfidentialBox.Core.DTOs;

namespace ConfidentialBox.Infrastructure.Services;

public interface ISecurePdfSessionPolicyStore
{
    void Store(string sessionId, PDFViewerSettingsDto settings, int maxViewMinutes);
    bool TryGet(string sessionId, out SecurePdfSessionPolicy? policy);
    void Remove(string sessionId);
}

public sealed record SecurePdfSessionPolicy(
    PDFViewerSettingsDto Settings,
    int MaxViewMinutes,
    DateTimeOffset StoredAt
);
