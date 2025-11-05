using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfidentialBox.Core.DTOs
{
    public class FileSearchRequest
    {
        public string? SearchTerm { get; set; }
        public DateTime? UploadedAfter { get; set; }
        public DateTime? UploadedBefore { get; set; }
        public string? UploadedByUserId { get; set; }
        public bool? IsBlocked { get; set; }
        public bool? IsDeleted { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
