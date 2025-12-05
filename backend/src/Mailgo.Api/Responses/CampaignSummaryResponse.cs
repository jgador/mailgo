using Mailgo.Domain.Enums;

namespace Mailgo.Api.Responses;

public record CampaignSummaryResponse(
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

