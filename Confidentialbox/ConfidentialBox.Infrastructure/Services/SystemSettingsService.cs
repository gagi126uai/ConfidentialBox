using System;
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

    public async Task<bool> IsUserRegistrationEnabledAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Category == SecurityCategory && s.Key == RegistrationEnabledKey, cancellationToken);

        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return true;
        }

        return bool.TryParse(setting.Value, out var value) ? value : true;
    }

    public async Task UpdateUserRegistrationEnabledAsync(bool isEnabled, string? updatedByUserId, CancellationToken cancellationToken = default)
    {
        await UpsertSettingAsync(SecurityCategory, RegistrationEnabledKey, isEnabled.ToString(), false, updatedByUserId, cancellationToken);
    }

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
