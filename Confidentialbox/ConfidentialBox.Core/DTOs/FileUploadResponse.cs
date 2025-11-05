using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfidentialBox.Core.DTOs
{
    public class FileUploadResponse
    {
        public bool Success { get; set; }
        public int FileId { get; set; }
        public string ShareLink { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
