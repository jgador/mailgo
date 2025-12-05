using Mailgo.Domain.Enums;

namespace Mailgo.Api.Responses;

public record CampaignSendLogResponse(
    Guid Id,
    Guid CampaignId,
    Guid RecipientId,
    string RecipientEmail,
    CampaignSendStatus Status,
    string? ErrorMessage,
    DateTime? SentAt);

