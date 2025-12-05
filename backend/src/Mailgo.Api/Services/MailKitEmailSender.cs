// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Mailgo.Api.Enums;
using Mailgo.Domain.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Mailgo.Api.Services;

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

        var fromName = string.IsNullOrWhiteSpace(settings.OverrideFromName)
            ? campaign.FromName
            : settings.OverrideFromName;
        var fromAddress = string.IsNullOrWhiteSpace(settings.OverrideFromAddress)
            ? campaign.FromEmail
            : settings.OverrideFromAddress;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = campaign.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = string.IsNullOrWhiteSpace(campaign.HtmlBody) ? null : campaign.HtmlBody,
            TextBody = string.IsNullOrWhiteSpace(campaign.TextBody) ? null : campaign.TextBody
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        if (settings.AllowSelfSignedCertificates || !string.IsNullOrWhiteSpace(settings.EncryptionHostname))
        {
            client.ServerCertificateValidationCallback = (_, certificate, _, errors) =>
            {
                if (settings.AllowSelfSignedCertificates)
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(settings.EncryptionHostname) && certificate is X509Certificate2 cert)
                {
                    var dnsName = cert.GetNameInfo(X509NameType.DnsName, false);
                    if (!string.IsNullOrWhiteSpace(dnsName) &&
                        dnsName.Equals(settings.EncryptionHostname, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return errors == SslPolicyErrors.None;
            };
        }

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

