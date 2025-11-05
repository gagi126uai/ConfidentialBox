using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfidentialBox.Core.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalFiles { get; set; }
        public int ActiveFiles { get; set; }
        public int ExpiredFiles { get; set; }
        public int BlockedFiles { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public long TotalStorageBytes { get; set; }
        public int TotalAccesses { get; set; }
        public int UnauthorizedAccesses { get; set; }
        public List<RecentActivityDto> RecentActivity { get; set; } = new();
    }
}
