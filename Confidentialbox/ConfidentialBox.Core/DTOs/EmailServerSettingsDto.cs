using System.ComponentModel.DataAnnotations;

namespace ConfidentialBox.Core.DTOs;

public class EmailServerSettingsDto
{
    [Required]
    [StringLength(256)]
    public string SmtpHost { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    [Required]
    [StringLength(256)]
    public string Username { get; set; } = string.Empty;

    [EmailAddress]
    public string? FromEmail { get; set; }

    [StringLength(128)]
    public string? FromName { get; set; }

    public bool HasPassword { get; set; }

    [StringLength(256)]
    public string? NewPassword { get; set; }
}
