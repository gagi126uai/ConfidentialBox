using ConfidentialBox.Core.Entities;
using ConfidentialBox.Core.DTOs;

namespace ConfidentialBox.Infrastructure.Services;

public interface IAISecurityService
{
    Task<FileThreatAnalysisDto> AnalyzeFileAsync(SharedFile file, string userId);
    Task<UserBehaviorAnalysisDto> AnalyzeUserBehaviorAsync(string userId);
    Task<AIScanSummaryDto> DetectAnomaliesAsync();
    Task<AISecurityDashboardDto> GetSecurityDashboardAsync();
    Task UpdateUserBehaviorProfileAsync(string userId);
    Task<bool> IncreaseMonitoringLevelAsync(string userId, int? desiredLevel, string? reason, string? updatedByUserId);
    Task<int> TransferAlertsToNewOwnerAsync(int fileId, string newOwnerId, string? actorUserId, CancellationToken cancellationToken = default);
}
