using System.ComponentModel.DataAnnotations;

namespace ConfidentialBox.Core.DTOs;

public class TokenSettingsDto
{
    [Range(1, 168, ErrorMessage = "La duración del token debe estar entre 1 y 168 horas.")]
    public int TokenLifetimeHours { get; set; } = 12;
}
