// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mailgo.Domain.Enums;

namespace Mailgo.Domain.Entities;

public class Campaign
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(256)]
    public string FromName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string FromEmail { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    public string? TextBody { get; set; }

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Snapshot of how many recipients were targeted when the latest send started.
    /// </summary>
    public int TargetRecipientCount { get; set; }

    public ICollection<CampaignSendLog> SendLogs { get; set; } = new List<CampaignSendLog>();
}

