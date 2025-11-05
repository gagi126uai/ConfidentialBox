namespace ConfidentialBox.Core.Configuration;

public class EmailServerSettings
{
    public string? SmtpHost { get; set; }
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public bool HasPassword => !string.IsNullOrWhiteSpace(Password);
}
