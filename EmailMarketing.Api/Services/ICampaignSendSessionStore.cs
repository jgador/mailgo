namespace EmailMarketing.Api.Services;

public interface ICampaignSendSessionStore
{
    void Upsert(CampaignSendSession session);
    bool TryGet(Guid campaignId, out CampaignSendSession? session);
    void Remove(Guid campaignId);
}
