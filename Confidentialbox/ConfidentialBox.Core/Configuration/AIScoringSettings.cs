using System;
using System.Collections.Generic;

namespace ConfidentialBox.Core.Configuration;

public class AIScoringSettings
{
    public double HighRiskThreshold { get; set; } = 0.7;
    public double SuspiciousThreshold { get; set; } = 0.5;
    public double SuspiciousExtensionScore { get; set; } = 0.3;
    public double LargeFileScore { get; set; } = 0.2;
    public double OutsideBusinessHoursScore { get; set; } = 0.15;
    public double UnusualUploadsScore { get; set; } = 0.25;
    public double UnusualFileSizeScore { get; set; } = 0.2;
    public double OutsideHoursBehaviorScore { get; set; } = 0.2;
    public double UnusualActivityIncrement { get; set; } = 0.1;
    public double MalwareProbabilityWeight { get; set; } = 0.4;
    public double DataExfiltrationWeight { get; set; } = 0.3;
    public int BusinessHoursStart { get; set; } = 7;
    public int BusinessHoursEnd { get; set; } = 20;
    public double UploadAnomalyMultiplier { get; set; } = 3.0;
    public double FileSizeAnomalyMultiplier { get; set; } = 2.0;
    public int MaxFileSizeMB { get; set; } = 100;
    public double MalwareSuspiciousExtensionWeight { get; set; } = 0.5;
    public double MalwareCrackKeywordWeight { get; set; } = 0.3;
    public double MalwareKeygenKeywordWeight { get; set; } = 0.3;
    public double MalwareExecutableWeight { get; set; } = 0.2;
    public int DataExfiltrationLargeFileMB { get; set; } = 50;
    public int DataExfiltrationHugeFileMB { get; set; } = 100;
    public double DataExfiltrationLargeFileWeight { get; set; } = 0.3;
    public double DataExfiltrationHugeFileWeight { get; set; } = 0.3;
    public double DataExfiltrationArchiveWeight { get; set; } = 0.2;
    public double DataExfiltrationOffHoursWeight { get; set; } = 0.2;
    public double RecommendationBlockThreshold { get; set; } = 0.8;
    public double RecommendationReviewThreshold { get; set; } = 0.6;
    public double RecommendationMonitorThreshold { get; set; } = 0.4;
    public double RiskLevelHighThreshold { get; set; } = 0.7;
    public double RiskLevelMediumThreshold { get; set; } = 0.4;
    public List<string> SuspiciousExtensions { get; set; } = new() { ".exe", ".bat", ".cmd", ".ps1", ".vbs", ".js" };
    public string PlatformTimeZone { get; set; } = TimeZoneInfo.Utc.Id;
}
