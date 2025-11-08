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
    Task<RegistrationSettingsDto?> GetRegistrationSettingsAsync();
    Task<bool> UpdateRegistrationSettingsAsync(bool isEnabled);
    Task<TokenSettingsDto?> GetTokenSettingsAsync();
    Task<bool> UpdateTokenSettingsAsync(TokenSettingsDto request);
    Task<AIScoringSettingsDto?> GetAIScoringSettingsAsync();
    Task<bool> UpdateAIScoringSettingsAsync(AIScoringSettingsDto request);
    Task<PDFViewerSettingsDto?> GetPdfViewerSettingsAsync();
    Task<bool> UpdatePdfViewerSettingsAsync(PDFViewerSettingsDto request);
}
