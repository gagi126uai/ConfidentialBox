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
}
