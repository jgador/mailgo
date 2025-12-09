// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Mailgo.Domain.Enums;

namespace Mailgo.Api.Responses;

public class CampaignSummaryResponse
{
    public CampaignSummaryResponse(
        Guid id,
        string name,
        string subject,
        string fromName,
        string fromEmail,
        CampaignStatus status,
        int totalRecipients,
        int sentCount,
        int failedCount,
        DateTime createdAt,
        DateTime lastUpdatedAt)
    {
        Id = id;
        Name = name;
        Subject = subject;
        FromName = fromName;
        FromEmail = fromEmail;
        Status = status;
        TotalRecipients = totalRecipients;
        SentCount = sentCount;
        FailedCount = failedCount;
        CreatedAt = createdAt;
        LastUpdatedAt = lastUpdatedAt;
    }

    [JsonPropertyName("id")]
    public Guid Id { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("subject")]
    public string Subject { get; }

    [JsonPropertyName("fromName")]
    public string FromName { get; }

    [JsonPropertyName("fromEmail")]
    public string FromEmail { get; }

    [JsonPropertyName("status")]
    public CampaignStatus Status { get; }

    [JsonPropertyName("totalRecipients")]
    public int TotalRecipients { get; }

    [JsonPropertyName("sentCount")]
    public int SentCount { get; }

    [JsonPropertyName("failedCount")]
    public int FailedCount { get; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; }

    [JsonPropertyName("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; }
}

