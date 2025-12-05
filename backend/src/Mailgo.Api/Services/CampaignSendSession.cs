namespace Mailgo.Api.Services;

public record CampaignSendSession(Guid CampaignId, SmtpSettings Settings, DateTime CreatedAt);

