using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfidentialBox.Core.DTOs
{
    public class RecentActivityDto
    {
        public string Action { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
