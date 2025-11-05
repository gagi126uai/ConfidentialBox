using ConfidentialBox.Core.Entities;
using ConfidentialBox.Core.DTOs;
using ConfidentialBox.Infrastructure.Data;
using ConfidentialBox.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ConfidentialBox.Infrastructure.Services;

public class AISecurityService : IAISecurityService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileRepository _fileRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    // Umbrales de detección
    private const double HIGH_RISK_THRESHOLD = 0.7;
    private const double SUSPICIOUS_THRESHOLD = 0.5;
    private const int MAX_FILES_PER_DAY_THRESHOLD = 50;
    private const long MAX_FILE_SIZE_MB = 100;
    private static readonly string[] SUSPICIOUS_EXTENSIONS = { ".exe", ".bat", ".cmd", ".ps1", ".vbs", ".js" };

    public AISecurityService(
        ApplicationDbContext context,
        IFileRepository fileRepository,
        IAuditLogRepository auditLogRepository)
    {
        _context = context;
        _fileRepository = fileRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<FileThreatAnalysisDto> AnalyzeFileAsync(SharedFile file, string userId)
    {
        var threatScore = 0.0;
        var threats = new List<string>();

        // 1. Análisis de extensión sospechosa
        var hasSuspiciousExtension = SUSPICIOUS_EXTENSIONS.Contains(file.FileExtension.ToLower());
        if (hasSuspiciousExtension)
        {
            threatScore += 0.3;
            threats.Add("Extensión de archivo potencialmente peligrosa");
        }

        // 2. Análisis de tamaño
        var fileSizeMB = file.FileSizeBytes / (1024.0 * 1024.0);
        if (fileSizeMB > MAX_FILE_SIZE_MB)
        {
            threatScore += 0.2;
            threats.Add($"Tamaño de archivo inusualmente grande: {fileSizeMB:F2} MB");
        }

        // 3. Análisis de hora de subida
        var uploadHour = file.UploadedAt.Hour;
        var isOutsideBusinessHours = uploadHour < 7 || uploadHour > 20;
        if (isOutsideBusinessHours)
        {
            threatScore += 0.15;
            threats.Add("Archivo subido fuera del horario laboral");
        }

        // 4. Análisis de comportamiento del usuario
        var userProfile = await _context.UserBehaviorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (userProfile != null)
        {
            var userFilesToday = await _fileRepository.GetByUserIdAsync(userId);
            var filesToday = userFilesToday.Count(f => f.UploadedAt.Date == DateTime.UtcNow.Date);

            if (filesToday > userProfile.AverageFilesPerDay * 3)
            {
                threatScore += 0.25;
                threats.Add("Número inusual de archivos subidos hoy");
            }
        }

        // 5. Simulación de ML - Detección de patrones (en producción usarías ML.NET)
        var malwareProbability = CalculateMalwareProbability(file, hasSuspiciousExtension);
        var dataExfiltrationProbability = CalculateDataExfiltrationProbability(file, fileSizeMB);

        threatScore = Math.Min(1.0, threatScore + (malwareProbability * 0.4) + (dataExfiltrationProbability * 0.3));

        // Guardar resultado del escaneo
        var scanResult = new FileScanResult
        {
            SharedFileId = file.Id,
            ScannedAt = DateTime.UtcNow,
            IsSuspicious = threatScore >= SUSPICIOUS_THRESHOLD,
            ThreatScore = threatScore,
            HasSuspiciousExtension = hasSuspiciousExtension,
            ExceedsSizeThreshold = fileSizeMB > MAX_FILE_SIZE_MB,
            UploadedOutsideBusinessHours = isOutsideBusinessHours,
            FileHash = GenerateFileHash(file),
            DetectedFileType = file.FileExtension,
            MalwareProbability = malwareProbability,
            DataExfiltrationProbability = dataExfiltrationProbability,
            AnalysisDetails = JsonSerializer.Serialize(new { threats, threatScore })
        };

        _context.FileScanResults.Add(scanResult);

        // Crear alerta de seguridad si es necesario
        if (threatScore >= SUSPICIOUS_THRESHOLD)
        {
            var severity = threatScore >= HIGH_RISK_THRESHOLD ? "High" : "Medium";
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
            IsThreat = threatScore >= SUSPICIOUS_THRESHOLD,
            ThreatScore = threatScore,
            Threats = threats,
            MalwareProbability = malwareProbability,
            DataExfiltrationProbability = dataExfiltrationProbability,
            Recommendation = GetRecommendation(threatScore)
        };
    }

    public async Task<UserBehaviorAnalysisDto> AnalyzeUserBehaviorAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("Usuario no encontrado");

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

        // Detectar anomalías
        var hasUnusualUploadPattern = filesToday > profile!.AverageFilesPerDay * 3;
        if (hasUnusualUploadPattern)
        {
            anomalies.Add($"Patrón de subida inusual: {filesToday} archivos hoy (promedio: {profile.AverageFilesPerDay:F1})");
        }

        var hasUnusualFileSize = currentAvgFileSize > profile.AverageFileSizeMB * 2;
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

        // Calcular risk score
        var riskScore = 0.0;
        if (hasUnusualUploadPattern) riskScore += 0.3;
        if (hasUnusualFileSize) riskScore += 0.2;
        if (accessingOutsideHours) riskScore += 0.2;
        riskScore += profile.UnusualActivityCount * 0.1;
        riskScore = Math.Min(1.0, riskScore);

        // Actualizar perfil con nuevo risk score
        profile.RiskScore = riskScore;
        if (anomalies.Any())
        {
            profile.UnusualActivityCount++;
            profile.LastUnusualActivity = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        // Crear alerta si el riesgo es alto
        if (riskScore >= HIGH_RISK_THRESHOLD)
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
            RiskLevel = GetRiskLevel(riskScore),
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
        var today = DateTime.UtcNow.Date;
        var alerts = await _context.SecurityAlerts
            .Include(a => a.User)
            .Include(a => a.File)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync();

        var alertsToday = alerts.Count(a => a.DetectedAt.Date == today);
        var criticalUnreviewed = alerts.Count(a => a.Severity == "Critical" && !a.IsReviewed);

        var highRiskUsers = await _context.UserBehaviorProfiles
            .Where(p => p.RiskScore >= HIGH_RISK_THRESHOLD)
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

    private double CalculateMalwareProbability(SharedFile file, bool hasSuspiciousExtension)
    {
        var probability = 0.0;

        if (hasSuspiciousExtension) probability += 0.5;
        if (file.OriginalFileName.Contains("crack", StringComparison.OrdinalIgnoreCase)) probability += 0.3;
        if (file.OriginalFileName.Contains("keygen", StringComparison.OrdinalIgnoreCase)) probability += 0.3;
        if (file.FileExtension.Equals(".exe", StringComparison.OrdinalIgnoreCase)) probability += 0.2;

        return Math.Min(1.0, probability);
    }

    private double CalculateDataExfiltrationProbability(SharedFile file, double fileSizeMB)
    {
        var probability = 0.0;

        if (fileSizeMB > 50) probability += 0.3;
        if (fileSizeMB > 100) probability += 0.3;
        if (file.FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
            file.FileExtension.Equals(".rar", StringComparison.OrdinalIgnoreCase)) probability += 0.2;
        if (file.UploadedAt.Hour < 6 || file.UploadedAt.Hour > 22) probability += 0.2;

        return Math.Min(1.0, probability);
    }

    private string GetRecommendation(double threatScore)
    {
        if (threatScore >= 0.8) return "BLOQUEAR INMEDIATAMENTE - Amenaza crítica detectada";
        if (threatScore >= 0.6) return "REVISAR MANUALMENTE - Alto riesgo detectado";
        if (threatScore >= 0.4) return "MONITOREAR - Actividad sospechosa";
        return "PERMITIR - Sin amenazas detectadas";
    }

    private string GetRiskLevel(double riskScore)
    {
        if (riskScore >= 0.7) return "High";
        if (riskScore >= 0.4) return "Medium";
        return "Low";
    }

    private string GenerateFileHash(SharedFile file)
    {
        return $"{file.Id}_{file.OriginalFileName}_{file.FileSizeBytes}".GetHashCode().ToString("X");
    }
}