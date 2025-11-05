using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfidentialBox.Core.DTOs
{
    public class CreateRoleRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public Dictionary<string, string> Policies { get; set; } = new();
    }

}
