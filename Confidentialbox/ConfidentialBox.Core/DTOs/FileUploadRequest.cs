using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfidentialBox.Core.DTOs
{
    public class FileUploadRequest
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string EncryptedFileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string EncryptionKey { get; set; } = string.Empty;
        public string EncryptedContent { get; set; } = string.Empty;
        public string? MasterPassword { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxAccessCount { get; set; }
        public bool EnableWatermark { get; set; }
        public string? WatermarkText { get; set; }
        public bool ScreenshotProtection { get; set; }
        public bool PrintProtection { get; set; }
        public bool CopyProtection { get; set; }
        public bool AiMonitoring { get; set; }
        public int MaxViewTimeMinutes { get; set; }
        public List<string> AllowedRoles { get; set; } = new();
    }
}
