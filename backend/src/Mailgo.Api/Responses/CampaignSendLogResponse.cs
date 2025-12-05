// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Mailgo.Domain.Enums;

namespace Mailgo.Api.Responses;

public class CampaignSendLogResponse
{
    public CampaignSendLogResponse(
        Guid id,
        Guid campaignId,
        Guid recipientId,
        string recipientEmail,
        CampaignSendStatus status,
        string? errorMessage,
        DateTime? sentAt)
    {
        Id = id;
        CampaignId = campaignId;
        RecipientId = recipientId;
        RecipientEmail = recipientEmail;
        Status = status;
        ErrorMessage = errorMessage;
        SentAt = sentAt;
    }

    [JsonPropertyName("id")]
    public Guid Id { get; }

    [JsonPropertyName("campaignId")]
    public Guid CampaignId { get; }

    [JsonPropertyName("recipientId")]
    public Guid RecipientId { get; }

    [JsonPropertyName("recipientEmail")]
    public string RecipientEmail { get; }

    [JsonPropertyName("status")]
    public CampaignSendStatus Status { get; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; }

    [JsonPropertyName("sentAt")]
    public DateTime? SentAt { get; }
}

