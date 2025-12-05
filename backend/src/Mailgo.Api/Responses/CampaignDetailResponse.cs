using EmailMarketing.Domain.Enums;

namespace EmailMarketing.Api.Responses;

public record CampaignDetailResponse(
    Guid Id,
    string Name,
    string Subject,
    string FromName,
    string FromEmail,
    string HtmlBody,
    string? TextBody,
    CampaignStatus Status,
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    DateTime CreatedAt,
    DateTime LastUpdatedAt);
