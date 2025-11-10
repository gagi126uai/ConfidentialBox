using System.Collections.Generic;

namespace ConfidentialBox.Core.DTOs;

public class AlertSummaryDto
{
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public Dictionary<string, int> SeverityCounts { get; set; } = new();
    public int NewAlerts { get; set; }
}
