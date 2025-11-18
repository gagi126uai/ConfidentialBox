using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.Entities;
using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Infrastructure.Data;
using ConfidentialBox.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ConfidentialBox.Infrastructure.Services;

public class AISecurityService : IAISecurityService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileRepository _fileRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISystemSettingsService _systemSettingsService;

    public AISecurityService(
        ApplicationDbContext context,
        IFileRepository fileRepository,
        IAuditLogRepository auditLogRepository,
        ISystemSettingsService systemSettingsService)
    {
        _context = context;
        _fileRepository = fileRepository;
        _auditLogRepository = auditLogRepository;
        _systemSettingsService = systemSettingsService;
    }

    private async Task<HashSet<string>> ResolveWhitelistedUsersAsync(AIScoringSettings scoring)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in scoring.WhitelistedUserIds ?? new List<string>())
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id.Trim());
            }
        }

        if (scoring.AdminBypassEnabled)
        {
            var adminIds = await _context.UserRoles
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == "Admin")
                .Select(x => x.UserId)
                .ToListAsync();

            foreach (var adminId in adminIds)
            {
                ids.Add(adminId);
            }
        }

        return ids;
    }

    private async Task<bool> IsUserWhitelistedAsync(string userId, AIScoringSettings scoring)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var whitelisted = await ResolveWhitelistedUsersAsync(scoring);
        return whitelisted.Contains(userId);
    }

    private async Task RecordWhitelistBypassAsync(string userId, string category, string? entityId = null)
    {
        var audit = new AuditLog
        {
            UserId = userId,
            Action = $"AI-Agent bypass ({category})",
            EntityType = "AI-Agent",
            EntityId = entityId,
            Timestamp = DateTime.UtcNow,
            IpAddress = "ai-agent",
            UserAgent = "AI-Agent whitelist",
            DeviceType = "AI-Agent",
            OperatingSystem = "AI-Agent"
        };

        await _auditLogRepository.AddAsync(audit);
    }

    public async Task<FileThreatAnalysisDto> AnalyzeFileAsync(SharedFile file, string userId)
    {
        var scoring = await _systemSettingsService.GetAIScoringSettingsAsync();
        var threatScore = 0.0;
        var threats = new List<string>();
        var zone = ResolveTimeZone(scoring.PlatformTimeZone);
        var localNow = GetPlatformNow(zone);

        if (await IsUserWhitelistedAsync(userId, scoring))
        {
            await RecordWhitelistBypassAsync(userId, "FileAnalysis", file.Id.ToString());
            return new FileThreatAnalysisDto
            {
                FileId = file.Id,
                FileName = file.OriginalFileName,
                IsThreat = false,
                ThreatScore = 0,
                Threats = new List<string> { "AI-Agent en modo lista blanca para este usuario." },
                MalwareProbability = 0,
                DataExfiltrationProbability = 0,
                Recommendation = "PERMITIR - Usuario en lista blanca"
            };
        }

        var normalizedExtension = NormalizeExtension(file.FileExtension);
        var suspiciousExtensions = new HashSet<string>(scoring.SuspiciousExtensions ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        var hasSuspiciousExtension = !string.IsNullOrEmpty(normalizedExtension) && suspiciousExtensions.Contains(normalizedExtension);
        if (hasSuspiciousExtension)
        {
            threatScore += scoring.SuspiciousExtensionScore;
            threats.Add("Extensión de archivo potencialmente peligrosa");
        }

        var fileSizeMB = file.FileSizeBytes / (1024.0 * 1024.0);
        if (fileSizeMB > scoring.MaxFileSizeMB)
        {
            threatScore += scoring.LargeFileScore;
            threats.Add($"Tamaño de archivo inusualmente grande: {fileSizeMB:F2} MB");
        }

        var localUpload = ConvertToPlatformTime(zone, file.UploadedAt);
        var uploadHour = localUpload.Hour;
        var isOutsideBusinessHours = uploadHour < scoring.BusinessHoursStart || uploadHour > scoring.BusinessHoursEnd;
        if (isOutsideBusinessHours)
        {
            threatScore += scoring.OutsideBusinessHoursScore;
            threats.Add("Archivo subido fuera del horario laboral");
        }

        var userProfile = await _context.UserBehaviorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (userProfile != null)
        {
            var userFilesToday = await _fileRepository.GetByUserIdAsync(userId);
            var localToday = localNow.Date;
            var filesToday = userFilesToday.Count(f => ConvertToPlatformTime(zone, f.UploadedAt).Date == localToday);

            if (filesToday > userProfile.AverageFilesPerDay * scoring.UploadAnomalyMultiplier)
            {
                threatScore += scoring.UnusualUploadsScore;
                threats.Add("Número inusual de archivos subidos hoy");
            }
        }

        var malwareProbability = CalculateMalwareProbability(file, hasSuspiciousExtension, scoring);
        var dataExfiltrationProbability = CalculateDataExfiltrationProbability(file, fileSizeMB, scoring);

        threatScore = Math.Min(1.0, threatScore + (malwareProbability * scoring.MalwareProbabilityWeight) + (dataExfiltrationProbability * scoring.DataExfiltrationWeight));

        var scanResult = new FileScanResult
        {
            SharedFileId = file.Id,
            ScannedAt = DateTime.UtcNow,
            IsSuspicious = threatScore >= scoring.SuspiciousThreshold,
            ThreatScore = threatScore,
            HasSuspiciousExtension = hasSuspiciousExtension,
            ExceedsSizeThreshold = fileSizeMB > scoring.MaxFileSizeMB,
            UploadedOutsideBusinessHours = isOutsideBusinessHours,
            FileHash = GenerateFileHash(file),
            DetectedFileType = file.FileExtension,
            MalwareProbability = malwareProbability,
            DataExfiltrationProbability = dataExfiltrationProbability,
            AnalysisDetails = JsonSerializer.Serialize(new { threats, threatScore })
        };

        _context.FileScanResults.Add(scanResult);

        if (threatScore >= scoring.SuspiciousThreshold)
        {
            var severity = threatScore >= scoring.HighRiskThreshold ? "High" : "Medium";
            await CreateSecurityAlert(
                "SuspiciousFile",
                severity,
                userId,
                file.Id,
                $"Archivo sospechoso detectado: {string.Join(", ", threats)}",
                "AI File Analysis",
                threatScore
            );
        }

        await _context.SaveChangesAsync();

        return new FileThreatAnalysisDto
        {
            FileId = file.Id,
            FileName = file.OriginalFileName,
            IsThreat = threatScore >= scoring.SuspiciousThreshold,
            ThreatScore = threatScore,
            Threats = threats,
            MalwareProbability = malwareProbability,
            DataExfiltrationProbability = dataExfiltrationProbability,
            Recommendation = GetRecommendation(threatScore, scoring)
        };
    }

    public async Task<UserBehaviorAnalysisDto> AnalyzeUserBehaviorAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("Usuario no encontrado");

        var scoring = await _systemSettingsService.GetAIScoringSettingsAsync();
        var zone = ResolveTimeZone(scoring.PlatformTimeZone);

        if (await IsUserWhitelistedAsync(userId, scoring))
        {
            await RecordWhitelistBypassAsync(userId, "UserBehavior");
            return new UserBehaviorAnalysisDto
            {
                UserId = userId,
                UserName = $"{user.FirstName} {user.LastName}",
                RiskScore = 0,
                RiskLevel = "Low",
                AnomaliesDetected = new List<string> { "Usuario en lista blanca AI-Agent" },
                LastAnalyzed = DateTime.UtcNow,
                AverageFilesPerDay = 0,
                CurrentFilesPerDay = 0,
                HasUnusualUploadPattern = false,
                HasUnusualAccessPattern = false,
                AccessingOutsideHours = false,
                IsWhitelisted = true
            };
        }

        var profile = await _context.UserBehaviorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            await UpdateUserBehaviorProfileAsync(userId);
            profile = await _context.UserBehaviorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        }

        var anomalies = new List<string>();
        var files = await _fileRepository.GetByUserIdAsync(userId);
        var localNow = GetPlatformNow(zone);
        var localToday = localNow.Date;
        var filesToday = files.Count(f => ConvertToPlatformTime(zone, f.UploadedAt).Date == localToday);
        var currentAvgFileSize = files.Any() ? files.Average(f => f.FileSizeBytes) / (1024.0 * 1024.0) : 0;

        var hasUnusualUploadPattern = filesToday > profile!.AverageFilesPerDay * scoring.UploadAnomalyMultiplier;
        if (hasUnusualUploadPattern)
        {
            anomalies.Add($"Patrón de subida inusual: {filesToday} archivos hoy (promedio: {profile.AverageFilesPerDay:F1})");
        }

        var hasUnusualFileSize = currentAvgFileSize > profile.AverageFileSizeMB * scoring.FileSizeAnomalyMultiplier;
        if (hasUnusualFileSize)
        {
            anomalies.Add($"Tamaño de archivo inusual: {currentAvgFileSize:F2} MB (promedio: {profile.AverageFileSizeMB:F2} MB)");
        }

        var now = localNow.TimeOfDay;
        var accessingOutsideHours = now < profile.TypicalActiveHoursStart || now > profile.TypicalActiveHoursEnd;
        if (accessingOutsideHours)
        {
            anomalies.Add("Acceso fuera del horario habitual");
        }

        var riskScore = 0.0;
        if (hasUnusualUploadPattern) riskScore += scoring.UnusualUploadsScore;
        if (hasUnusualFileSize) riskScore += scoring.UnusualFileSizeScore;
        if (accessingOutsideHours) riskScore += scoring.OutsideHoursBehaviorScore;
        riskScore += profile.UnusualActivityCount * scoring.UnusualActivityIncrement;
        riskScore = Math.Min(1.0, riskScore);

        profile.RiskScore = riskScore;
        if (anomalies.Any())
        {
            profile.UnusualActivityCount++;
            profile.LastUnusualActivity = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        if (riskScore >= scoring.HighRiskThreshold)
        {
            await CreateSecurityAlert(
                "BehavioralAnomaly",
                "High",
                userId,
                null,
                $"Comportamiento anómalo detectado: {string.Join(", ", anomalies)}",
                "AI Behavior Analysis",
                riskScore
            );
        }

        return new UserBehaviorAnalysisDto
        {
            UserId = userId,
            UserName = $"{user.FirstName} {user.LastName}",
            RiskScore = riskScore,
            RiskLevel = GetRiskLevel(riskScore, scoring),
            AnomaliesDetected = anomalies,
            LastAnalyzed = localNow,
            AverageFilesPerDay = profile.AverageFilesPerDay,
            CurrentFilesPerDay = filesToday,
            HasUnusualUploadPattern = hasUnusualUploadPattern,
            HasUnusualAccessPattern = hasUnusualFileSize,
            AccessingOutsideHours = accessingOutsideHours,
            IsWhitelisted = false
        };
    }

    public async Task<AIScanSummaryDto> DetectAnomaliesAsync()
    {
        var scanStarted = DateTime.UtcNow;
        var users = await _context.Users.Where(u => u.IsActive).ToListAsync();

        foreach (var user in users)
        {
            try
            {
                await AnalyzeUserBehaviorAsync(user.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing user {user.Id}: {ex.Message}");
            }
        }

        var scoring = await _systemSettingsService.GetAIScoringSettingsAsync();
        var whitelisted = await ResolveWhitelistedUsersAsync(scoring);
        var newAlerts = await _context.SecurityAlerts
            .Where(a => a.DetectedAt >= scanStarted)
            .CountAsync();

        var highRiskProfiles = await _context.UserBehaviorProfiles
            .Where(p => p.RiskScore >= scoring.HighRiskThreshold && !whitelisted.Contains(p.UserId))
            .CountAsync();

        return new AIScanSummaryDto
        {
            ExecutedAtUtc = DateTime.UtcNow,
            AlertsGenerated = newAlerts,
            HighRiskProfilesReviewed = highRiskProfiles,
            Message = newAlerts > 0
                ? $"Se generaron {newAlerts} alertas nuevas durante el escaneo."
                : "Escaneo completado sin nuevas alertas."
        };
    }

    public async Task<AISecurityDashboardDto> GetSecurityDashboardAsync()
    {
        var scoring = await _systemSettingsService.GetAIScoringSettingsAsync();
        var zone = ResolveTimeZone(scoring.PlatformTimeZone);
        var localNow = GetPlatformNow(zone);
        var today = localNow.Date;
        var dayStartUtc = ConvertToUtc(zone, today);
        var nextDayUtc = dayStartUtc.AddDays(1);
        var whitelisted = await ResolveWhitelistedUsersAsync(scoring);
        var alerts = await _context.SecurityAlerts
            .Include(a => a.User)
            .Include(a => a.File)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();

        bool AlertVisible(SecurityAlert alert) => string.IsNullOrWhiteSpace(alert.UserId) || !whitelisted.Contains(alert.UserId);

        var visibleAlerts = alerts.Where(AlertVisible).ToList();

        var alertsToday = visibleAlerts.Count(a => ConvertToPlatformTime(zone, a.DetectedAt).Date == today);
        var criticalUnreviewed = visibleAlerts.Count(a => a.Severity == "Critical" && !a.IsReviewed);

        var highRiskProfilesRaw = await _context.UserBehaviorProfiles
            .Include(p => p.User)
            .Where(p => p.RiskScore >= scoring.HighRiskThreshold)
            .OrderByDescending(p => p.RiskScore)
            .ToListAsync();

        var highRiskProfiles = highRiskProfilesRaw
            .Where(p => !whitelisted.Contains(p.UserId))
            .ToList();
        var highRiskUsers = highRiskProfiles.Count;
        var highRiskProfilesTop = highRiskProfiles
            .OrderByDescending(p => p.RiskScore)
            .Take(5)
            .ToList();

        var highRiskDetails = new List<UserBehaviorAnalysisDto>();
        foreach (var profile in highRiskProfilesTop)
        {
            var userFiles = await _fileRepository.GetByUserIdAsync(profile.UserId);
            var todaysFiles = userFiles.Count(f => ConvertToPlatformTime(zone, f.UploadedAt).Date == today);
            var anomalies = BuildProfileInsights(profile, zone);
            var riskLevel = GetRiskLevel(profile.RiskScore, scoring);

            highRiskDetails.Add(new UserBehaviorAnalysisDto
            {
                UserId = profile.UserId,
                UserName = profile.User != null ? $"{profile.User.FirstName} {profile.User.LastName}" : profile.UserId,
                RiskScore = profile.RiskScore,
                RiskLevel = riskLevel,
                AverageFilesPerDay = profile.AverageFilesPerDay,
                CurrentFilesPerDay = todaysFiles,
                HasUnusualUploadPattern = todaysFiles > profile.AverageFilesPerDay * scoring.UploadAnomalyMultiplier,
                HasUnusualAccessPattern = profile.UnusualActivityCount > 0,
                AccessingOutsideHours = false,
                AnomaliesDetected = anomalies,
                LastAnalyzed = ConvertToPlatformTime(zone, profile.LastUpdated),
                IsWhitelisted = false
            });
        }

        var suspiciousFiles = await _context.FileScanResults
            .Where(s => s.IsSuspicious && s.ScannedAt >= dayStartUtc && s.ScannedAt < nextDayUtc)
            .CountAsync();

        var last24HoursStartUtc = ConvertToUtc(zone, localNow.AddHours(-24));
        var last24Hours = visibleAlerts
            .Where(a => a.DetectedAt >= last24HoursStartUtc)
            .ToList();

        var systemThreatLevel = last24Hours.Any()
            ? last24Hours.Average(a => a.ConfidenceScore)
            : 0.0;

        var recentAlerts = visibleAlerts.Take(10).Select(a => new SecurityAlertDto
        {
            Id = a.Id,
            AlertType = a.AlertType,
            Severity = a.Severity,
            UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : "Usuario desconocido",
            FileName = a.File?.OriginalFileName,
            Description = a.Description,
            ConfidenceScore = a.ConfidenceScore,
            DetectedAt = ConvertToPlatformTime(zone, a.DetectedAt),
            IsReviewed = a.IsReviewed,
            ActionTaken = a.ActionTaken
        }).ToList();

        var alertsByType = visibleAlerts
            .Where(a => a.DetectedAt >= dayStartUtc && a.DetectedAt < nextDayUtc)
            .GroupBy(a => a.AlertType)
            .ToDictionary(g => g.Key, g => g.Count());

        var threatTrends = new Dictionary<string, int>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var trendStartUtc = ConvertToUtc(zone, date);
            var trendEndUtc = trendStartUtc.AddDays(1);
            var count = alerts.Count(a => a.DetectedAt >= trendStartUtc && a.DetectedAt < trendEndUtc);
            threatTrends[date.ToString("dd/MM")] = count;
        }

        var recommendations = BuildRecommendations(alertsToday, criticalUnreviewed, highRiskUsers, last24Hours.Count);

        return new AISecurityDashboardDto
        {
            TotalAlertsToday = alertsToday,
            CriticalAlertsUnreviewed = criticalUnreviewed,
            HighRiskUsers = highRiskUsers,
            SuspiciousFilesDetected = suspiciousFiles,
            SystemThreatLevel = systemThreatLevel,
            RecentAlerts = recentAlerts,
            AlertsByType = alertsByType,
            ThreatTrends = threatTrends,
            HighRiskUsersDetails = highRiskDetails,
            ActionRecommendations = recommendations
        };
    }

    public async Task<int> TransferAlertsToNewOwnerAsync(int fileId, string newOwnerId, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var alerts = await _context.SecurityAlerts
            .Where(a => a.FileId == fileId)
            .ToListAsync(cancellationToken);

        if (alerts.Count == 0)
        {
            return 0;
        }

        foreach (var alert in alerts)
        {
            var previousOwnerId = alert.UserId;
            alert.UserId = newOwnerId;

            var metadata = JsonSerializer.Serialize(new
            {
                fileId,
                previousOwnerId,
                newOwnerId,
                transferredAt = DateTime.UtcNow
            });

            _context.SecurityAlertActions.Add(new SecurityAlertAction
            {
                AlertId = alert.Id,
                ActionType = "OwnershipTransferred",
                Notes = "Reasignación automática por cambio de propietario del archivo.",
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = actorUserId,
                TargetUserId = newOwnerId,
                TargetFileId = fileId,
                StatusAfterAction = alert.Status
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return alerts.Count;
    }

    public async Task UpdateUserBehaviorProfileAsync(string userId)
    {
        var scoring = await _systemSettingsService.GetAIScoringSettingsAsync();
        if (await IsUserWhitelistedAsync(userId, scoring))
        {
            await RecordWhitelistBypassAsync(userId, "BehaviorProfile");
            return;
        }

        var zone = ResolveTimeZone(scoring.PlatformTimeZone);
        var localNow = GetPlatformNow(zone);

        var files = await _fileRepository.GetByUserIdAsync(userId);
        var accesses = await _context.FileAccesses
            .Where(a => a.AccessedByUserId == userId)
            .ToListAsync();

        var profile = await _context.UserBehaviorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var firstUploadUtc = files.Any() ? files.Min(f => EnsureUtc(f.UploadedAt)) : DateTime.UtcNow;
        var firstUploadLocal = ConvertToPlatformTime(zone, firstUploadUtc);
        var daysSinceFirstFile = Math.Max(1, (localNow - firstUploadLocal).TotalDays);

        var avgFilesPerDay = files.Count / daysSinceFirstFile;
        var avgFileSizeMB = files.Any() ? files.Average(f => f.FileSizeBytes) / (1024.0 * 1024.0) : 0;

        var accessTimes = accesses
            .Select(a => ConvertToPlatformTime(zone, a.AccessedAt).TimeOfDay)
            .ToList();
        var typicalStart = accessTimes.Any() ? TimeSpan.FromHours(accessTimes.Min(t => t.TotalHours)) : TimeSpan.FromHours(9);
        var typicalEnd = accessTimes.Any() ? TimeSpan.FromHours(accessTimes.Max(t => t.TotalHours)) : TimeSpan.FromHours(18);

        var commonFileTypes = files
            .GroupBy(f => f.FileExtension)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        if (profile == null)
        {
            profile = new UserBehaviorProfile
            {
                UserId = userId,
                ProfileCreatedAt = DateTime.UtcNow
            };
            _context.UserBehaviorProfiles.Add(profile);
        }

        profile.AverageFilesPerDay = avgFilesPerDay;
        profile.AverageFileSizeMB = avgFileSizeMB;
        profile.TypicalActiveHoursStart = typicalStart;
        profile.TypicalActiveHoursEnd = typicalEnd;
        profile.CommonFileTypes = JsonSerializer.Serialize(commonFileTypes);
        profile.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> IncreaseMonitoringLevelAsync(string userId, int? desiredLevel, string? reason, string? updatedByUserId)
    {
        var profile = await _context.UserBehaviorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            await UpdateUserBehaviorProfileAsync(userId);
            profile = await _context.UserBehaviorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return false;
            }
        }

        var targetLevel = desiredLevel.HasValue
            ? Math.Clamp(desiredLevel.Value, 1, 5)
            : Math.Min(5, profile.MonitoringLevel + 1);

        if (targetLevel == profile.MonitoringLevel)
        {
            return true;
        }

        profile.MonitoringLevel = targetLevel;
        profile.MonitoringLevelUpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var prefix = string.IsNullOrWhiteSpace(profile.MonitoringNotes) ? string.Empty : profile.MonitoringNotes + "\n";
            profile.MonitoringNotes = prefix + $"[{DateTime.UtcNow:O}] Nivel {targetLevel} - {reason}";
        }

        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(updatedByUserId))
        {
            await _auditLogRepository.AddAsync(new AuditLog
            {
                UserId = updatedByUserId,
                Action = "MonitoringLevelChanged",
                EntityType = "UserBehaviorProfile",
                EntityId = userId,
                NewValues = $"MonitoringLevel={targetLevel}",
                Timestamp = DateTime.UtcNow
            });
        }

        return true;
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private List<string> BuildProfileInsights(UserBehaviorProfile profile, TimeZoneInfo zone)
    {
        var insights = new List<string>();

        if (profile.UnusualActivityCount > 0)
        {
            insights.Add($"Actividades inusuales registradas: {profile.UnusualActivityCount}");
        }

        if (profile.LastUnusualActivity.HasValue)
        {
            var local = ConvertToPlatformTime(zone, profile.LastUnusualActivity.Value);
            insights.Add($"Último evento fuera de patrón: {local:dd/MM HH:mm}");
        }

        if (!string.IsNullOrWhiteSpace(profile.CommonFileTypes))
        {
            try
            {
                var types = JsonSerializer.Deserialize<List<string>>(profile.CommonFileTypes) ?? new List<string>();
                if (types.Count > 0)
                {
                    insights.Add($"Tipos frecuentes: {string.Join(", ", types)}");
                }
            }
            catch
            {
                // ignore parsing issues
            }
        }

        return insights;
    }

    private static List<string> BuildRecommendations(int alertsToday, int criticalUnreviewed, int highRiskUsers, int alertsLast24Hours)
    {
        var recommendations = new List<string>();

        if (criticalUnreviewed > 0)
        {
            recommendations.Add($"Revisar de inmediato {criticalUnreviewed} alertas críticas pendientes.");
        }

        if (highRiskUsers > 0)
        {
            recommendations.Add($"Coordinar seguimiento con {highRiskUsers} perfiles de alto riesgo.");
        }

        if (alertsToday > Math.Max(5, alertsLast24Hours / 2))
        {
            recommendations.Add("Investigar incremento de alertas registradas durante el día.");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("Sin acciones urgentes. Mantén el monitoreo continuo.");
        }

        return recommendations;
    }

    private static DateTime EnsureUtc(DateTime dateTime)
        => dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            _ => dateTime.ToUniversalTime()
        };

    private static DateTime ConvertToPlatformTime(TimeZoneInfo zone, DateTime utcDateTime)
        => TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(utcDateTime), zone);

    private static DateTime ConvertToUtc(TimeZoneInfo zone, DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
    }

    private static DateTime GetPlatformNow(TimeZoneInfo zone)
        => ConvertToPlatformTime(zone, DateTime.UtcNow);

    private async Task CreateSecurityAlert(
        string alertType,
        string severity,
        string userId,
        int? fileId,
        string description,
        string detectedPattern,
        double confidenceScore)
    {
        var alert = new SecurityAlert
        {
            AlertType = alertType,
            Severity = severity,
            UserId = userId,
            FileId = fileId,
            Description = description,
            DetectedPattern = detectedPattern,
            ConfidenceScore = confidenceScore,
            DetectedAt = DateTime.UtcNow
        };

        _context.SecurityAlerts.Add(alert);
        await _context.SaveChangesAsync();
    }

    private double CalculateMalwareProbability(SharedFile file, bool hasSuspiciousExtension, AIScoringSettings scoring)
    {
        var probability = 0.0;

        if (hasSuspiciousExtension)
        {
            probability += scoring.MalwareSuspiciousExtensionWeight;
        }

        if (!string.IsNullOrEmpty(file.OriginalFileName) &&
            file.OriginalFileName.Contains("crack", StringComparison.OrdinalIgnoreCase))
        {
            probability += scoring.MalwareCrackKeywordWeight;
        }

        if (!string.IsNullOrEmpty(file.OriginalFileName) &&
            file.OriginalFileName.Contains("keygen", StringComparison.OrdinalIgnoreCase))
        {
            probability += scoring.MalwareKeygenKeywordWeight;
        }

        var normalizedExtension = NormalizeExtension(file.FileExtension);
        if (!string.IsNullOrEmpty(normalizedExtension) &&
            normalizedExtension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            probability += scoring.MalwareExecutableWeight;
        }

        return Math.Min(1.0, probability);
    }

    private double CalculateDataExfiltrationProbability(SharedFile file, double fileSizeMB, AIScoringSettings scoring)
    {
        var probability = 0.0;

        if (fileSizeMB > scoring.DataExfiltrationLargeFileMB)
        {
            probability += scoring.DataExfiltrationLargeFileWeight;
        }

        if (fileSizeMB > scoring.DataExfiltrationHugeFileMB)
        {
            probability += scoring.DataExfiltrationHugeFileWeight;
        }

        var normalizedExtension = NormalizeExtension(file.FileExtension);
        if (!string.IsNullOrEmpty(normalizedExtension) &&
            (normalizedExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
             normalizedExtension.Equals(".rar", StringComparison.OrdinalIgnoreCase)))
        {
            probability += scoring.DataExfiltrationArchiveWeight;
        }

        var uploadHour = file.UploadedAt.Hour;
        if (uploadHour < scoring.BusinessHoursStart || uploadHour > scoring.BusinessHoursEnd)
        {
            probability += scoring.DataExfiltrationOffHoursWeight;
        }

        return Math.Min(1.0, probability);
    }

    private string GetRecommendation(double threatScore, AIScoringSettings scoring)
    {
        if (threatScore >= scoring.RecommendationBlockThreshold) return "BLOQUEAR INMEDIATAMENTE - Amenaza crítica detectada";
        if (threatScore >= scoring.RecommendationReviewThreshold) return "REVISAR MANUALMENTE - Alto riesgo detectado";
        if (threatScore >= scoring.RecommendationMonitorThreshold) return "MONITOREAR - Actividad sospechosa";
        return "PERMITIR - Sin amenazas detectadas";
    }

    private string GetRiskLevel(double riskScore, AIScoringSettings scoring)
    {
        if (riskScore >= scoring.RiskLevelHighThreshold) return "High";
        if (riskScore >= scoring.RiskLevelMediumThreshold) return "Medium";
        return "Low";
    }

    private static string? NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var trimmed = extension.Trim();
        if (!trimmed.StartsWith("."))
        {
            trimmed = "." + trimmed;
        }

        return trimmed.ToLowerInvariant();
    }

    private string GenerateFileHash(SharedFile file)
    {
        return $"{file.Id}_{file.OriginalFileName}_{file.FileSizeBytes}".GetHashCode().ToString("X");
    }
}