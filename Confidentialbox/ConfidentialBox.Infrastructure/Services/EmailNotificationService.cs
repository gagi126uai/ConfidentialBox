using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Configuration;
using ConfidentialBox.Core.Entities;

namespace ConfidentialBox.Infrastructure.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly ISystemSettingsService _systemSettingsService;

    public EmailNotificationService(ISystemSettingsService systemSettingsService)
    {
        _systemSettingsService = systemSettingsService;
    }

    public async Task SendPasswordResetAsync(ApplicationUser user, string resetLink, CancellationToken cancellationToken = default)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("El usuario no tiene un correo electrónico configurado");
        }

        var serverSettings = await _systemSettingsService.GetEmailServerSettingsAsync(cancellationToken);
        ValidateServerSettings(serverSettings);

        var notificationSettings = await _systemSettingsService.GetEmailNotificationSettingsAsync(cancellationToken);

        var subject = "ConfidentialBox - Restablecer contraseña";
        var body = $@"<p>Hola {user.FirstName},</p>
<p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en <strong>ConfidentialBox</strong>.</p>
<p>Para continuar, haz clic en el siguiente enlace seguro (válido por un tiempo limitado):</p>
<p><a href=\"{resetLink}\">Restablecer contraseña</a></p>
<p>Si no solicitaste este cambio, puedes ignorar este mensaje, pero te recomendamos avisar al equipo de seguridad.</p>
<p>— Equipo ConfidentialBox</p>";

        await SendEmailAsync(
            serverSettings,
            subject,
            body,
            new[] { user.Email },
            notificationSettings.SendPasswordRecovery ? notificationSettings.PasswordRecoveryRecipients : null,
            cancellationToken);
    }

    public async Task SendSecurityAlertAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        var serverSettings = await _systemSettingsService.GetEmailServerSettingsAsync(cancellationToken);
        ValidateServerSettings(serverSettings);

        var notificationSettings = await _systemSettingsService.GetEmailNotificationSettingsAsync(cancellationToken);
        if (!notificationSettings.SendSecurityAlerts)
        {
            return;
        }

        await SendEmailAsync(
            serverSettings,
            subject,
            body,
            Enumerable.Empty<string>(),
            notificationSettings.SecurityAlertRecipients,
            cancellationToken);
    }

    private async Task SendEmailAsync(
        EmailServerSettings serverSettings,
        string subject,
        string body,
        IEnumerable<string> primaryRecipients,
        string? additionalRecipients,
        CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            From = BuildFromAddress(serverSettings)
        };

        foreach (var recipient in primaryRecipients)
        {
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                message.To.Add(recipient.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(additionalRecipients))
        {
            foreach (var recipient in additionalRecipients.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Bcc.Add(recipient.Trim());
            }
        }

        if (!message.To.Any() && !message.Bcc.Any())
        {
            // Sin destinatarios válidos
            return;
        }

        using var client = new SmtpClient(serverSettings.SmtpHost!, serverSettings.Port)
        {
            EnableSsl = serverSettings.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(serverSettings.Username) && !string.IsNullOrWhiteSpace(serverSettings.Password))
        {
            client.Credentials = new NetworkCredential(serverSettings.Username, serverSettings.Password);
        }
        else
        {
            client.UseDefaultCredentials = true;
        }

        await client.SendMailAsync(message, cancellationToken);
    }

    private MailAddress BuildFromAddress(EmailServerSettings serverSettings)
    {
        var address = serverSettings.FromEmail;
        if (string.IsNullOrWhiteSpace(address))
        {
            address = serverSettings.Username;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            address = "no-reply@confidentialbox.local";
        }

        var display = string.IsNullOrWhiteSpace(serverSettings.FromName) ? "ConfidentialBox" : serverSettings.FromName;
        return new MailAddress(address!, display);
    }

    private static void ValidateServerSettings(EmailServerSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            throw new InvalidOperationException("El servidor SMTP no está configurado");
        }

        if (settings.Port <= 0)
        {
            throw new InvalidOperationException("El puerto SMTP configurado no es válido");
        }
    }
}
