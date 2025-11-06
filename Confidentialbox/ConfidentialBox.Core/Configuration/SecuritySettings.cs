namespace ConfidentialBox.Core.Configuration;

public class SecuritySettings
{
    public int TokenLifetimeHours { get; set; } = 12;
    public bool UserRegistrationEnabled { get; set; } = true;
}
