namespace ConfidentialBox.Core.Configuration;

public class EmailServerSettings
{
    public string? SmtpHost { get; set; }
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public bool UseStartTls { get; set; } = true;
    public bool ValidateCertificates { get; set; } = true;
    public bool AllowSelfSignedCertificates { get; set; }
    public bool RequireTls12 { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string? ReplyToEmail { get; set; }
    public string? ReplyToName { get; set; }
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public string AuthenticationMechanism { get; set; } = "Auto";
    public string? SecondaryHost { get; set; }
    public int SecondaryPort { get; set; } = 587;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryBackoffSeconds { get; set; } = 15;
    public bool EnableDeliveryStatusNotifications { get; set; } = true;
    public bool TrackBounceReasons { get; set; } = true;
    public bool UseClientCertificate { get; set; }
    public string? ClientCertificateThumbprint { get; set; }
    public string? OperationalContactEmail { get; set; }
    public bool HasPassword => !string.IsNullOrWhiteSpace(Password);
}
