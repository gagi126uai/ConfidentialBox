using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Configuration;

namespace ConfidentialBox.Infrastructure.Services;

public interface ISystemSettingsService
{
    Task<FileStorageSettings> GetFileStorageSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateFileStorageSettingsAsync(FileStorageSettings settings, string? updatedByUserId, CancellationToken cancellationToken = default);

    Task<EmailServerSettings> GetEmailServerSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateEmailServerSettingsAsync(EmailServerSettings settings, string? updatedByUserId, CancellationToken cancellationToken = default);

    Task<EmailNotificationSettings> GetEmailNotificationSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateEmailNotificationSettingsAsync(EmailNotificationSettings settings, string? updatedByUserId, CancellationToken cancellationToken = default);
    Task<bool> IsUserRegistrationEnabledAsync(CancellationToken cancellationToken = default);
    Task UpdateUserRegistrationEnabledAsync(bool isEnabled, string? updatedByUserId, CancellationToken cancellationToken = default);
    Task<SecuritySettings> GetSecuritySettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateSecuritySettingsAsync(SecuritySettings settings, string? updatedByUserId, CancellationToken cancellationToken = default);
    Task<AIScoringSettings> GetAIScoringSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateAIScoringSettingsAsync(AIScoringSettings settings, string? updatedByUserId, CancellationToken cancellationToken = default);
    Task<PDFViewerSettings> GetPdfViewerSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdatePdfViewerSettingsAsync(PDFViewerSettings settings, string? updatedByUserId, CancellationToken cancellationToken = default);
}
