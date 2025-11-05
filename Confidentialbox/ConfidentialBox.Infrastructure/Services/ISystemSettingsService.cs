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
}
