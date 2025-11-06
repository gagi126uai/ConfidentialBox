using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ConfidentialBox.Infrastructure.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private const string StorageCategory = "Storage";
    private const string EmailServerCategory = "EmailServer";
    private const string EmailNotificationCategory = "EmailNotifications";
    private const string SecurityCategory = "Security";
    private const string RegistrationEnabledKey = "UserRegistrationEnabled";
    private const string TokenLifetimeKey = "TokenLifetimeHours";
    private const string AIScoringCategory = "AI";
    private const string AIScoringSettingsKey = "ScoringSettings";

    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly FileStorageSettings _defaultStorageSettings;

    public SystemSettingsService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IConfiguration configuration,
        IOptions<FileStorageSettings> defaultStorageOptions)
    {
        _context = context;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _defaultStorageSettings = defaultStorageOptions.Value;
    }

    public async Task<FileStorageSettings> GetFileStorageSettingsAsync(CancellationToken cancellationToken = default)
    {
        var result = new FileStorageSettings
        {
            StoreInDatabase = _defaultStorageSettings.StoreInDatabase,
            StoreOnFileSystem = _defaultStorageSettings.StoreOnFileSystem,
            FileSystemDirectory = _defaultStorageSettings.FileSystemDirectory
        };

        var settings = await _context.SystemSettings
            .Where(s => s.Category == StorageCategory)
            .ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            switch (setting.Key)
            {
                case "StoreInDatabase" when bool.TryParse(setting.Value, out var dbValue):
                    result.StoreInDatabase = dbValue;
                    break;
                case "StoreOnFileSystem" when bool.TryParse(setting.Value, out var fsValue):
                    result.StoreOnFileSystem = fsValue;
                    break;
                case "FileSystemDirectory" when !string.IsNullOrWhiteSpace(setting.Value):
                    result.FileSystemDirectory = setting.Value;
                    break;
            }
        }

        return result;
    }

    public async Task UpdateFileStorageSettingsAsync(FileStorageSettings settings, string? updatedByUserId, CancellationToken cancellationToken = default)
    {
        var directory = settings.FileSystemDirectory?.Trim() ?? string.Empty;
        await UpsertSettingAsync(StorageCategory, "StoreInDatabase", settings.StoreInDatabase.ToString(), false, updatedByUserId, cancellationToken);
        await UpsertSettingAsync(StorageCategory, "StoreOnFileSystem", settings.StoreOnFileSystem.ToString(), false, updatedByUserId, cancellationToken);
        await UpsertSettingAsync(StorageCategory, "FileSystemDirectory", directory, false, updatedByUserId, cancellationToken);
    }

    public async Task<EmailServerSettings> GetEmailServerSettingsAsync(CancellationToken cancellationToken = default)
    {
        var result = new EmailServerSettings
        {
            Port = 587,
            UseSsl = true
        };

        var settings = await _context.SystemSettings
            .Where(s => s.Category == EmailServerCategory)
            .ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            switch (setting.Key)
            {
                case "SmtpHost":
                    result.SmtpHost = setting.Value;
                    break;
                case "Port" when int.TryParse(setting.Value, out var port):
                    result.Port = port;
                    break;
                case "UseSsl" when bool.TryParse(setting.Value, out var useSsl):
                    result.UseSsl = useSsl;
                    break;
                case "Username":
                    result.Username = setting.Value;
                    break;
                case "Password" when !string.IsNullOrWhiteSpace(setting.Value):
                    result.Password = DecryptSetting(setting.Value);
                    break;
                case "FromEmail":
                    result.FromEmail = setting.Value;
                    break;
                case "FromName":
                    result.FromName = setting.Value;
                    break;
            }
        }

        return result;
    }

    public async Task UpdateEmailServerSettingsAsync(EmailServerSettings settings, string? updatedByUserId, CancellationToken cancellationToken = default)
    {
        settings.SmtpHost = settings.SmtpHost?.Trim();
        settings.Username = settings.Username?.Trim();
        settings.FromEmail = settings.FromEmail?.Trim();
        settings.FromName = settings.FromName?.Trim();

        await UpsertSettingAsync(EmailServerCategory, "SmtpHost", settings.SmtpHost ?? string.Empty, false, updatedByUserId, cancellationToken);
        await UpsertSettingAsync(EmailServerCategory, "Port", settings.Port.ToString(), false, updatedByUserId, cancellationToken);
        await UpsertSettingAsync(EmailServerCategory, "UseSsl", settings.UseSsl.ToString(), false, updatedByUserId, cancellationToken);
        await UpsertSettingAsync(EmailServerCategory, "Username", settings.Username ?? string.Empty, false, updatedByUserId, cancellationToken);
        await UpsertSettingAsync(EmailServerCategory, "FromEmail", settings.FromEmail ?? string.Empty, false, updatedByUserId, cancellationToken);
        await UpsertSettingAsync(EmailServerCategory, "FromName", settings.FromName ?? string.Empty, false, updatedByUserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Password))
        {
            await UpsertSettingAsync(EmailServerCategory, "Password", settings.Password, true, updatedByUserId, cancellationToken);
        }
    }

    public async Task<EmailNotificationSettings> GetEmailNotificationSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Category == EmailNotificationCategory && s.Key == "Channels", cancellationToken);

        if (settings == null || string.IsNullOrWhiteSpace(settings.Value))
        {
            return new EmailNotificationSettings();
        }

        try
        {
            var result = JsonSerializer.Deserialize<EmailNotificationSettings>(settings.Value);
            return result ?? new EmailNotificationSettings();
        }
        catch
        {
            return new EmailNotificationSettings();
        }
    }

    public async Task UpdateEmailNotificationSettingsAsync(EmailNotificationSettings settings, string? updatedByUserId, CancellationToken cancellationToken = default)
    {
        settings.SecurityAlertRecipients = settings.SecurityAlertRecipients?.Trim() ?? string.Empty;
        settings.PasswordRecoveryRecipients = settings.PasswordRecoveryRecipients?.Trim() ?? string.Empty;
        settings.UserInvitationRecipients = settings.UserInvitationRecipients?.Trim() ?? string.Empty;

        var serialized = JsonSerializer.Serialize(settings);
        await UpsertSettingAsync(EmailNotificationCategory, "Channels", serialized, false, updatedByUserId, cancellationToken);
    }

    public async Task<SecuritySettings> GetSecuritySettingsAsync(CancellationToken cancellationToken = default)
    {
        var result = new SecuritySettings();

        var settings = await _context.SystemSettings
            .AsNoTracking()
            .Where(s => s.Category == SecurityCategory && (s.Key == RegistrationEnabledKey || s.Key == TokenLifetimeKey))
            .ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            switch (setting.Key)
            {
                case RegistrationEnabledKey when bool.TryParse(setting.Value, out var isEnabled):
                    result.UserRegistrationEnabled = isEnabled;
                    break;
                case TokenLifetimeKey when int.TryParse(setting.Value, out var hours) && hours > 0:
                    result.TokenLifetimeHours = hours;
                    break;
            }
        }

        return NormalizeSecuritySettings(result);
    }

    public async Task UpdateSecuritySettingsAsync(SecuritySettings settings, string? updatedByUserId, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSecuritySettings(settings);

        await UpsertSettingAsync(SecurityCategory, RegistrationEnabledKey, normalized.UserRegistrationEnabled.ToString(), false, updatedByUserId, cancellationToken);
        await UpsertSettingAsync(SecurityCategory, TokenLifetimeKey, normalized.TokenLifetimeHours.ToString(), false, updatedByUserId, cancellationToken);
    }

    public async Task<AIScoringSettings> GetAIScoringSettingsAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Category == AIScoringCategory && s.Key == AIScoringSettingsKey, cancellationToken);

        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return NormalizeAIScoringSettings(new AIScoringSettings());
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<AIScoringSettings>(setting.Value);
            return NormalizeAIScoringSettings(deserialized ?? new AIScoringSettings());
        }
        catch
        {
            return NormalizeAIScoringSettings(new AIScoringSettings());
        }
    }

    public async Task UpdateAIScoringSettingsAsync(AIScoringSettings settings, string? updatedByUserId, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAIScoringSettings(settings);
        var serialized = JsonSerializer.Serialize(normalized);
        await UpsertSettingAsync(AIScoringCategory, AIScoringSettingsKey, serialized, false, updatedByUserId, cancellationToken);
    }

    public async Task<bool> IsUserRegistrationEnabledAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSecuritySettingsAsync(cancellationToken);
        return settings.UserRegistrationEnabled;
    }

    public async Task UpdateUserRegistrationEnabledAsync(bool isEnabled, string? updatedByUserId, CancellationToken cancellationToken = default)
    {
        var settings = await GetSecuritySettingsAsync(cancellationToken);
        settings.UserRegistrationEnabled = isEnabled;
        await UpdateSecuritySettingsAsync(settings, updatedByUserId, cancellationToken);
    }

    private static SecuritySettings NormalizeSecuritySettings(SecuritySettings settings)
    {
        settings.TokenLifetimeHours = Math.Clamp(settings.TokenLifetimeHours, 1, 168);
        return settings;
    }

    private static AIScoringSettings NormalizeAIScoringSettings(AIScoringSettings settings)
    {
        settings.HighRiskThreshold = Clamp01(settings.HighRiskThreshold);
        settings.SuspiciousThreshold = Clamp01(settings.SuspiciousThreshold);
        settings.SuspiciousExtensionScore = Clamp01(settings.SuspiciousExtensionScore);
        settings.LargeFileScore = Clamp01(settings.LargeFileScore);
        settings.OutsideBusinessHoursScore = Clamp01(settings.OutsideBusinessHoursScore);
        settings.UnusualUploadsScore = Clamp01(settings.UnusualUploadsScore);
        settings.UnusualFileSizeScore = Clamp01(settings.UnusualFileSizeScore);
        settings.OutsideHoursBehaviorScore = Clamp01(settings.OutsideHoursBehaviorScore);
        settings.UnusualActivityIncrement = Clamp01(settings.UnusualActivityIncrement);
        settings.MalwareProbabilityWeight = Clamp01(settings.MalwareProbabilityWeight);
        settings.DataExfiltrationWeight = Clamp01(settings.DataExfiltrationWeight);
        settings.MalwareSuspiciousExtensionWeight = Clamp01(settings.MalwareSuspiciousExtensionWeight);
        settings.MalwareCrackKeywordWeight = Clamp01(settings.MalwareCrackKeywordWeight);
        settings.MalwareKeygenKeywordWeight = Clamp01(settings.MalwareKeygenKeywordWeight);
        settings.MalwareExecutableWeight = Clamp01(settings.MalwareExecutableWeight);
        settings.DataExfiltrationLargeFileWeight = Clamp01(settings.DataExfiltrationLargeFileWeight);
        settings.DataExfiltrationHugeFileWeight = Clamp01(settings.DataExfiltrationHugeFileWeight);
        settings.DataExfiltrationArchiveWeight = Clamp01(settings.DataExfiltrationArchiveWeight);
        settings.DataExfiltrationOffHoursWeight = Clamp01(settings.DataExfiltrationOffHoursWeight);
        settings.RecommendationMonitorThreshold = Clamp01(settings.RecommendationMonitorThreshold);
        settings.RecommendationReviewThreshold = Math.Clamp(settings.RecommendationReviewThreshold, settings.RecommendationMonitorThreshold, 1.0);
        settings.RecommendationBlockThreshold = Math.Clamp(settings.RecommendationBlockThreshold, settings.RecommendationReviewThreshold, 1.0);
        settings.RiskLevelMediumThreshold = Clamp01(settings.RiskLevelMediumThreshold);
        settings.RiskLevelHighThreshold = Math.Clamp(settings.RiskLevelHighThreshold, settings.RiskLevelMediumThreshold, 1.0);

        settings.BusinessHoursStart = Math.Clamp(settings.BusinessHoursStart, 0, 23);
        settings.BusinessHoursEnd = Math.Clamp(settings.BusinessHoursEnd, settings.BusinessHoursStart, 23);
        settings.UploadAnomalyMultiplier = Math.Max(1.0, settings.UploadAnomalyMultiplier);
        settings.FileSizeAnomalyMultiplier = Math.Max(1.0, settings.FileSizeAnomalyMultiplier);
        settings.MaxFileSizeMB = Math.Max(1, settings.MaxFileSizeMB);
        settings.DataExfiltrationLargeFileMB = Math.Max(1, settings.DataExfiltrationLargeFileMB);
        settings.DataExfiltrationHugeFileMB = Math.Max(settings.DataExfiltrationLargeFileMB, settings.DataExfiltrationHugeFileMB);

        settings.SuspiciousExtensions = NormalizeExtensions(settings.SuspiciousExtensions);

        return settings;
    }

    private static List<string> NormalizeExtensions(IEnumerable<string> extensions)
    {
        return extensions
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e =>
            {
                var trimmed = e.Trim();
                if (!trimmed.StartsWith('.'))
                {
                    trimmed = "." + trimmed;
                }
                return trimmed.ToLowerInvariant();
            })
            .Distinct()
            .ToList();
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0.0, 1.0);

    private async Task UpsertSettingAsync(string category, string key, string value, bool isSensitive, string? updatedByUserId, CancellationToken cancellationToken)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Category == category && s.Key == key, cancellationToken);

        var storedValue = isSensitive && !string.IsNullOrEmpty(value)
            ? EncryptSetting(value)
            : value;

        if (setting == null)
        {
            setting = new SystemSetting
            {
                Category = category,
                Key = key,
                Value = storedValue,
                IsSensitive = isSensitive,
                UpdatedAt = DateTime.UtcNow,
                UpdatedByUserId = updatedByUserId
            };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            if (isSensitive && string.IsNullOrEmpty(value))
            {
                // No update to sensitive value if empty provided
            }
            else
            {
                setting.Value = storedValue;
            }

            setting.IsSensitive = isSensitive;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedByUserId = updatedByUserId;
            _context.SystemSettings.Update(setting);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private string EncryptSetting(string value)
    {
        var key = GetEncryptionKey();
        return _encryptionService.Encrypt(value, key);
    }

    private string DecryptSetting(string value)
    {
        var key = GetEncryptionKey();
        return _encryptionService.Decrypt(value, key);
    }

    private string GetEncryptionKey()
    {
        var key = _configuration["SystemSecrets:SettingsEncryptionKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Missing SystemSecrets:SettingsEncryptionKey configuration");
        }

        return key;
    }
}
