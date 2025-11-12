using System.Collections.Generic;

namespace ConfidentialBox.Core.DTOs;

public class SecurityAlertDetailDto
{
    public SecurityAlertDto Alert { get; set; } = new();
    public List<SecurityAlertActionDto> Actions { get; set; } = new();
    public bool CanBlockFile { get; set; }
    public bool CanBlockUser { get; set; }
    public bool CanEscalateMonitoring { get; set; }
    public FileAccessLogDto? LatestAccess { get; set; }
}
