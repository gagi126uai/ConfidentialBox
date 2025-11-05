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
        public string? MasterPassword { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxAccessCount { get; set; }
        public List<string> AllowedRoles { get; set; } = new();
    }
}
