using System.Threading;
using System.Threading.Tasks;
using ConfidentialBox.Core.Entities;

namespace ConfidentialBox.Infrastructure.Services;

public interface IEmailNotificationService
{
    Task SendPasswordResetAsync(ApplicationUser user, string resetLink, CancellationToken cancellationToken = default);
    Task SendSecurityAlertAsync(string subject, string body, CancellationToken cancellationToken = default);
}
