// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Mailgo.Domain.Entities;

namespace Mailgo.Api.Responses;

public static class ResponseMapper
{
    public static RecipientResponse ToResponse(this Recipient recipient) =>
        new(
            recipient.Id,
            recipient.Email,
            recipient.FirstName,
            recipient.LastName,
            recipient.CreatedAt);

    public static CampaignSummaryResponse ToSummaryResponse(
        this Campaign campaign,
        int totalRecipients,
        int sentCount,
        int failedCount) =>
        new(
            campaign.Id,
            campaign.Name,
            campaign.Subject,
            campaign.FromName,
            campaign.FromEmail,
            campaign.Status,
            totalRecipients,
            sentCount,
            failedCount,
            campaign.CreatedAt,
            campaign.LastUpdatedAt);

    public static CampaignDetailResponse ToDetailResponse(
        this Campaign campaign,
        int totalRecipients,
        int sentCount,
        int failedCount) =>
        new(
            campaign.Id,
            campaign.Name,
            campaign.Subject,
            campaign.FromName,
            campaign.FromEmail,
            campaign.HtmlBody,
            campaign.TextBody,
            campaign.Status,
            totalRecipients,
            sentCount,
            failedCount,
            campaign.CreatedAt,
            campaign.LastUpdatedAt);

    public static CampaignSendLogResponse ToResponse(this CampaignSendLog log) =>
        new(
            log.Id,
            log.CampaignId,
            log.RecipientId,
            log.Recipient.Email,
            log.Status,
            log.ErrorMessage,
            log.SentAt);
}

