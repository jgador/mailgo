using System.Collections.Concurrent;

namespace Mailgo.Api.Services;

public class InMemoryCampaignSendSessionStore : ICampaignSendSessionStore
{
    private readonly ConcurrentDictionary<Guid, CampaignSendSession> _sessions = new();

    public void Upsert(CampaignSendSession session) =>
        _sessions.AddOrUpdate(session.CampaignId, session, (_, _) => session);

    public bool TryGet(Guid campaignId, out CampaignSendSession? session) =>
        _sessions.TryGetValue(campaignId, out session);

    public void Remove(Guid campaignId) =>
        _sessions.TryRemove(campaignId, out _);
}

