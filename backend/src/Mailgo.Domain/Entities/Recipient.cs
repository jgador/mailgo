// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Mailgo.Domain.Entities;

public class Recipient
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? FirstName { get; set; }

    [MaxLength(128)]
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CampaignSendLog> SendLogs { get; set; } = new List<CampaignSendLog>();
}

