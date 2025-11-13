using System.ComponentModel.DataAnnotations;

namespace ConfidentialBox.Core.DTOs;

public class UserMessageReplyRequest
{
    [Required]
    [MinLength(3)]
    public string Body { get; set; } = string.Empty;
}
