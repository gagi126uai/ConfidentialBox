using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.DTOs;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public interface ISettingsService
{
    Task<FileStorageSettings?> GetStorageSettingsAsync();
    Task<bool> UpdateStorageSettingsAsync(FileStorageSettings settings);
    Task<EmailServerSettingsDto?> GetEmailServerSettingsAsync();
    Task<bool> UpdateEmailServerSettingsAsync(EmailServerSettingsDto request);
    Task<EmailNotificationSettingsDto?> GetEmailNotificationSettingsAsync();
    Task<bool> UpdateEmailNotificationSettingsAsync(EmailNotificationSettingsDto request);
}
