using System.ComponentModel.DataAnnotations;

namespace ConfidentialBox.Core.DTOs;

public class EmailNotificationSettingsDto
{
    public bool SendSecurityAlerts { get; set; }

    [StringLength(1024)]
    public string SecurityAlertRecipients { get; set; } = string.Empty;

    public bool SendPasswordRecovery { get; set; }

    [StringLength(1024)]
    public string PasswordRecoveryRecipients { get; set; } = string.Empty;

    public bool SendUserInvitations { get; set; }

    [StringLength(1024)]
    public string UserInvitationRecipients { get; set; } = string.Empty;
}
