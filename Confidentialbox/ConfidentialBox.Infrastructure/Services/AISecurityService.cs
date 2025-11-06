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

    public async Task<FileThreatAnalysisDto> AnalyzeFileAsync(SharedFile file, string userId)
    {
        var scoring = await _systemSettingsService.GetAIScoringSettingsAsync();
        var threatScore = 0.0;
        var threats = new List<string>();

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

        var uploadHour = file.UploadedAt.Hour;
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
            var filesToday = userFilesToday.Count(f => f.UploadedAt.Date == DateTime.UtcNow.Date);

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

        var profile = await _context.UserBehaviorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            await UpdateUserBehaviorProfileAsync(userId);
            profile = await _context.UserBehaviorProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        }

        var anomalies = new List<string>();
        var files = await _fileRepository.GetByUserIdAsync(userId);
        var filesToday = files.Count(f => f.UploadedAt.Date == DateTime.UtcNow.Date);
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

        var now = DateTime.UtcNow.TimeOfDay;
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
            LastAnalyzed = DateTime.UtcNow,
            AverageFilesPerDay = profile.AverageFilesPerDay,
            CurrentFilesPerDay = filesToday,
            HasUnusualUploadPattern = hasUnusualUploadPattern,
            HasUnusualAccessPattern = hasUnusualFileSize,
            AccessingOutsideHours = accessingOutsideHours
        };
    }

    public async Task<List<SecurityAlert>> DetectAnomaliesAsync()
    {
        var alerts = new List<SecurityAlert>();
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

        return await _context.SecurityAlerts
            .Where(a => a.DetectedAt >= DateTime.UtcNow.AddHours(-24))
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();
    }

    public async Task<AISecurityDashboardDto> GetSecurityDashboardAsync()
    {
        var scoring = await _systemSettingsService.GetAIScoringSettingsAsync();
        var today = DateTime.UtcNow.Date;
        var alerts = await _context.SecurityAlerts
            .Include(a => a.User)
            .Include(a => a.File)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();

        var alertsToday = alerts.Count(a => a.DetectedAt.Date == today);
        var criticalUnreviewed = alerts.Count(a => a.Severity == "Critical" && !a.IsReviewed);

        var highRiskUsers = await _context.UserBehaviorProfiles
            .Where(p => p.RiskScore >= scoring.HighRiskThreshold)
            .CountAsync();

        var suspiciousFiles = await _context.FileScanResults
            .Where(s => s.IsSuspicious && s.ScannedAt >= today)
            .CountAsync();

        var systemThreatLevel = alerts.Any()
            ? alerts.Where(a => a.DetectedAt >= DateTime.UtcNow.AddHours(-24))
                    .Average(a => a.ConfidenceScore)
            : 0.0;

        var recentAlerts = alerts.Take(10).Select(a => new SecurityAlertDto
        {
            Id = a.Id,
            AlertType = a.AlertType,
            Severity = a.Severity,
            UserName = $"{a.User.FirstName} {a.User.LastName}",
            FileName = a.File?.OriginalFileName,
            Description = a.Description,
            ConfidenceScore = a.ConfidenceScore,
            DetectedAt = a.DetectedAt,
            IsReviewed = a.IsReviewed,
            ActionTaken = a.ActionTaken
        }).ToList();

        var alertsByType = alerts
            .Where(a => a.DetectedAt >= today)
            .GroupBy(a => a.AlertType)
            .ToDictionary(g => g.Key, g => g.Count());

        var threatTrends = new Dictionary<string, int>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i).Date;
            var count = alerts.Count(a => a.DetectedAt.Date == date);
            threatTrends[date.ToString("dd/MM")] = count;
        }

        return new AISecurityDashboardDto
        {
            TotalAlertsToday = alertsToday,
            CriticalAlertsUnreviewed = criticalUnreviewed,
            HighRiskUsers = highRiskUsers,
            SuspiciousFilesDetected = suspiciousFiles,
            SystemThreatLevel = systemThreatLevel,
            RecentAlerts = recentAlerts,
            AlertsByType = alertsByType,
            ThreatTrends = threatTrends
        };
    }

    public async Task UpdateUserBehaviorProfileAsync(string userId)
    {
        var files = await _fileRepository.GetByUserIdAsync(userId);
        var accesses = await _context.FileAccesses
            .Where(a => a.AccessedByUserId == userId)
            .ToListAsync();

        var profile = await _context.UserBehaviorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var daysSinceFirstFile = files.Any()
            ? (DateTime.UtcNow - files.Min(f => f.UploadedAt)).TotalDays
            : 1;
        daysSinceFirstFile = Math.Max(1, daysSinceFirstFile);

        var avgFilesPerDay = files.Count / daysSinceFirstFile;
        var avgFileSizeMB = files.Any() ? files.Average(f => f.FileSizeBytes) / (1024.0 * 1024.0) : 0;

        var accessTimes = accesses.Select(a => a.AccessedAt.TimeOfDay).ToList();
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