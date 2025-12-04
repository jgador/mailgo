using EmailMarketing.Domain.Enums;

namespace EmailMarketing.Api.Dtos;

public record CampaignSendLogDto(
    Guid Id,
    Guid CampaignId,
    Guid RecipientId,
    string RecipientEmail,
    CampaignSendStatus Status,
    string? ErrorMessage,
    DateTime? SentAt);
