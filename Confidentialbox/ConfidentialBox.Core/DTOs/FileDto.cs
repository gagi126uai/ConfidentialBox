using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfidentialBox.Core.DTOs
{
    public class FileDto
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ShareLink { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxAccessCount { get; set; }
        public int CurrentAccessCount { get; set; }
        public bool IsBlocked { get; set; }
        public string? BlockReason { get; set; }
        public string UploadedByUserName { get; set; } = string.Empty;
        public bool HasMasterPassword { get; set; }
    }
}
