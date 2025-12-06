// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mailgo.Api.Services;
using Mailgo.Api.Stores;
using Mailgo.Domain.Entities;
using Mailgo.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mailgo.Api.Background;

public class CampaignSenderService(
    IServiceScopeFactory scopeFactory,
    ICampaignSendSessionStore sessionStore,
    ILogger<CampaignSenderService> logger,
    IEmailSender emailSender) : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ActivePollInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var campaignStore = scope.ServiceProvider.GetRequiredService<CampaignStore>();

                var campaign = await campaignStore.GetNextSendingCampaignAsync(stoppingToken).ConfigureAwait(false);

                if (campaign is null)
                {
                    await Task.Delay(IdlePollInterval, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                if (!sessionStore.TryGet(campaign.Id, out var session) || session is null)
                {
                    logger.LogWarning("Missing SMTP session for campaign {CampaignId}. Marking as failed.", campaign.Id);
                    await campaignStore.MarkCampaignAsFailedAsync(campaign, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                var completed = await ProcessBatchAsync(campaignStore, campaign, session, stoppingToken).ConfigureAwait(false);

                if (!completed)
                {
                    await Task.Delay(ActivePollInterval, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in campaign sender loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(
        CampaignStore campaignStore,
        Campaign campaign,
        CampaignSendSession session,
        CancellationToken cancellationToken)
    {
        var pendingRecipients = await campaignStore
            .GetPendingRecipientsAsync(campaign.Id, BatchSize, cancellationToken)
            .ConfigureAwait(false);

        if (pendingRecipients.Count == 0)
        {
            await campaignStore.FinalizeCampaignAsync(campaign, cancellationToken).ConfigureAwait(false);
            sessionStore.Remove(campaign.Id);
            return true;
        }

        var logs = new List<CampaignSendLog>(pendingRecipients.Count);

        foreach (var recipient in pendingRecipients)
        {
            var log = new CampaignSendLog
            {
                CampaignId = campaign.Id,
                RecipientId = recipient.Id,
                Status = CampaignSendStatus.Sent,
                SentAt = DateTime.UtcNow
            };

            try
            {
                await emailSender.SendAsync(campaign, recipient, session.Settings, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Status = CampaignSendStatus.Failed;
                log.ErrorMessage = TruncateError(ex.Message);
                logger.LogError(ex, "Failed to send campaign {CampaignId} to {RecipientEmail}", campaign.Id, recipient.Email);
            }

            logs.Add(log);
        }

        await campaignStore.AddSendLogsAsync(logs, cancellationToken).ConfigureAwait(false);
        campaign.LastUpdatedAt = DateTime.UtcNow;
        await campaignStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return false;
    }

    private static string? TruncateError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        return message.Length <= 512 ? message : message[..512];
    }
}

