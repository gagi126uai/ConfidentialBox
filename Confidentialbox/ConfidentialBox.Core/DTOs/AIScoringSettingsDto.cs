using System.ComponentModel.DataAnnotations;

namespace ConfidentialBox.Core.DTOs;

public class AIScoringSettingsDto
{
    [Range(0.0, 1.0)]
    public double HighRiskThreshold { get; set; } = 0.7;

    [Range(0.0, 1.0)]
    public double SuspiciousThreshold { get; set; } = 0.5;

    [Range(0.0, 1.0)]
    public double SuspiciousExtensionScore { get; set; } = 0.3;

    [Range(0.0, 1.0)]
    public double LargeFileScore { get; set; } = 0.2;

    [Range(0.0, 1.0)]
    public double OutsideBusinessHoursScore { get; set; } = 0.15;

    [Range(0.0, 1.0)]
    public double UnusualUploadsScore { get; set; } = 0.25;

    [Range(0.0, 1.0)]
    public double UnusualFileSizeScore { get; set; } = 0.2;

    [Range(0.0, 1.0)]
    public double OutsideHoursBehaviorScore { get; set; } = 0.2;

    [Range(0.0, 1.0)]
    public double UnusualActivityIncrement { get; set; } = 0.1;

    [Range(0.0, 1.0)]
    public double MalwareProbabilityWeight { get; set; } = 0.4;

    [Range(0.0, 1.0)]
    public double DataExfiltrationWeight { get; set; } = 0.3;

    [Range(0, 23)]
    public int BusinessHoursStart { get; set; } = 7;

    [Range(0, 23)]
    public int BusinessHoursEnd { get; set; } = 20;

    [Range(1.0, 10.0)]
    public double UploadAnomalyMultiplier { get; set; } = 3.0;

    [Range(1.0, 10.0)]
    public double FileSizeAnomalyMultiplier { get; set; } = 2.0;

    [Range(1, 10240)]
    public int MaxFileSizeMB { get; set; } = 100;

    [Range(0.0, 1.0)]
    public double MalwareSuspiciousExtensionWeight { get; set; } = 0.5;

    [Range(0.0, 1.0)]
    public double MalwareCrackKeywordWeight { get; set; } = 0.3;

    [Range(0.0, 1.0)]
    public double MalwareKeygenKeywordWeight { get; set; } = 0.3;

    [Range(0.0, 1.0)]
    public double MalwareExecutableWeight { get; set; } = 0.2;

    [Range(1, 20480)]
    public int DataExfiltrationLargeFileMB { get; set; } = 50;

    [Range(1, 20480)]
    public int DataExfiltrationHugeFileMB { get; set; } = 100;

    [Range(0.0, 1.0)]
    public double DataExfiltrationLargeFileWeight { get; set; } = 0.3;

    [Range(0.0, 1.0)]
    public double DataExfiltrationHugeFileWeight { get; set; } = 0.3;

    [Range(0.0, 1.0)]
    public double DataExfiltrationArchiveWeight { get; set; } = 0.2;

    [Range(0.0, 1.0)]
    public double DataExfiltrationOffHoursWeight { get; set; } = 0.2;

    [Range(0.0, 1.0)]
    public double RecommendationBlockThreshold { get; set; } = 0.8;

    [Range(0.0, 1.0)]
    public double RecommendationReviewThreshold { get; set; } = 0.6;

    [Range(0.0, 1.0)]
    public double RecommendationMonitorThreshold { get; set; } = 0.4;

    [Range(0.0, 1.0)]
    public double RiskLevelHighThreshold { get; set; } = 0.7;

    [Range(0.0, 1.0)]
    public double RiskLevelMediumThreshold { get; set; } = 0.4;

    [Required]
    public string SuspiciousExtensions { get; set; } = ".exe, .bat, .cmd, .ps1, .vbs, .js";
}
