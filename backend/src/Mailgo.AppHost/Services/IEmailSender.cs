// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
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

