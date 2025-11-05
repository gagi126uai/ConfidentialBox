namespace ConfidentialBox.Core.Configuration;

public class EmailNotificationSettings
{
    public bool SendSecurityAlerts { get; set; } = true;
    public string SecurityAlertRecipients { get; set; } = string.Empty;
    public bool SendPasswordRecovery { get; set; } = true;
    public string PasswordRecoveryRecipients { get; set; } = string.Empty;
    public bool SendUserInvitations { get; set; } = false;
    public string UserInvitationRecipients { get; set; } = string.Empty;
}
