// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

namespace Mailgo.Api.Services;

public interface ICampaignSendSessionStore
{
    void Upsert(CampaignSendSession session);
    bool TryGet(Guid campaignId, out CampaignSendSession? session);
    void Remove(Guid campaignId);
}

