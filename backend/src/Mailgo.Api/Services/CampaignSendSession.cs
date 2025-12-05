// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

namespace Mailgo.Api.Services;

public record CampaignSendSession(Guid CampaignId, SmtpSettings Settings, DateTime CreatedAt);

