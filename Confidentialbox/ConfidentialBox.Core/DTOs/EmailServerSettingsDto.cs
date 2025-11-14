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
    public bool UseStartTls { get; set; } = true;
    public bool ValidateCertificates { get; set; } = true;

    [Required]
    [StringLength(256)]
    public string Username { get; set; } = string.Empty;

    [EmailAddress]
    public string? FromEmail { get; set; }

    [StringLength(128)]
    public string? FromName { get; set; }

    [EmailAddress]
    public string? ReplyToEmail { get; set; }

    [StringLength(128)]
    public string? ReplyToName { get; set; }

    [Range(5, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    [StringLength(64)]
    [RegularExpression("^(Auto|Plain|Login|CramMd5)$", ErrorMessage = "Selecciona un mecanismo de autenticación válido.")]
    public string AuthenticationMechanism { get; set; } = "Auto";

    public bool HasPassword { get; set; }

    [StringLength(256)]
    public string? NewPassword { get; set; }
}
