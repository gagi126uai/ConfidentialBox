using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfidentialBox.Core.DTOs
{
    public class PolicyDto
    {
        public int Id { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public string PolicyValue { get; set; } = string.Empty;
    }
}
