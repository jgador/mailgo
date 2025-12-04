using EmailMarketing.Domain.Entities;

namespace EmailMarketing.Api.Dtos;

public static class DtoMapper
{
    public static RecipientDto ToDto(this Recipient recipient) =>
        new(
            recipient.Id,
            recipient.Email,
            recipient.FirstName,
            recipient.LastName,
            recipient.CreatedAt);

    public static CampaignSummaryDto ToSummaryDto(
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

    public static CampaignDetailDto ToDetailDto(
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

    public static CampaignSendLogDto ToDto(this CampaignSendLog log) =>
        new(
            log.Id,
            log.CampaignId,
            log.RecipientId,
            log.Recipient.Email,
            log.Status,
            log.ErrorMessage,
            log.SentAt);
}
