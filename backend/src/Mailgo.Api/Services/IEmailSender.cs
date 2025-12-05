using Mailgo.Domain.Entities;

namespace Mailgo.Api.Services;

public interface IEmailSender
{
    Task SendAsync(
        Campaign campaign,
        Recipient recipient,
        SmtpSettings settings,
        CancellationToken cancellationToken,
        string? overrideRecipientEmail = null);
}

