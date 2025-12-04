using EmailMarketing.Api.Enums;
using EmailMarketing.Domain.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EmailMarketing.Api.Services;

public class MailKitEmailSender : IEmailSender
{
    public async Task SendAsync(
        Campaign campaign,
        Recipient recipient,
        SmtpSettings settings,
        CancellationToken cancellationToken,
        string? overrideRecipientEmail = null)
    {
        var toAddress = overrideRecipientEmail ?? recipient.Email;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(campaign.FromName, campaign.FromEmail));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = campaign.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = string.IsNullOrWhiteSpace(campaign.HtmlBody) ? null : campaign.HtmlBody,
            TextBody = string.IsNullOrWhiteSpace(campaign.TextBody) ? null : campaign.TextBody
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var secureOption = settings.Encryption switch
        {
            EncryptionType.SSL => SecureSocketOptions.SslOnConnect,
            EncryptionType.StartTls => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.None
        };

        await client.ConnectAsync(settings.Host, settings.Port, secureOption, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
