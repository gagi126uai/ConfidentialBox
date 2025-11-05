using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ConfidentialBox.Core.DTOs
{
    public class AISecurityDashboardDto
    {
        public int TotalAlertsToday { get; set; }
        public int CriticalAlertsUnreviewed { get; set; }

        // Mantiene la propiedad numérica "HighRiskUsers" en JSON
        public int HighRiskUsers { get; set; }

        public int SuspiciousFilesDetected { get; set; }

        [Range(0.0, 1.0)]
        public double SystemThreatLevel { get; set; }

        public List<SecurityAlertDto> RecentAlerts { get; set; } = new();

        // Nueva lista; en JSON se verá como "HighRiskUsersDetails"
        [JsonPropertyName("HighRiskUsersDetails")]
        public List<UserBehaviorAnalysisDto> HighRiskUsersDetails { get; set; } = new();

        public Dictionary<string, int> AlertsByType { get; set; } = new();
        public Dictionary<string, int> ThreatTrends { get; set; } = new();
    }
}
