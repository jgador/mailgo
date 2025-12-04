using EmailMarketing.Domain.Enums;

namespace EmailMarketing.Api.Dtos;

public record CampaignSummaryDto(
    Guid Id,
    string Name,
    string Subject,
    string FromName,
    string FromEmail,
    CampaignStatus Status,
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    DateTime CreatedAt,
    DateTime LastUpdatedAt);
