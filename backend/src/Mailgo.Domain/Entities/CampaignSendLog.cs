// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Mailgo.Domain.Enums;

namespace Mailgo.Domain.Entities;

public class CampaignSendLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public Guid RecipientId { get; set; }
    public Recipient Recipient { get; set; } = null!;

    public CampaignSendStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }
}

