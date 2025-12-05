using Mailgo.Api.Data;
using Mailgo.Api.Services;
using Mailgo.Domain.Entities;
using Mailgo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var campaign = await dbContext.Campaigns
                    .Where(c => c.Status == CampaignStatus.Sending)
                    .OrderBy(c => c.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                if (campaign is null)
                {
                    await Task.Delay(IdlePollInterval, stoppingToken);
                    continue;
                }

                if (!sessionStore.TryGet(campaign.Id, out var session) || session is null)
                {
                    logger.LogWarning("Missing SMTP session for campaign {CampaignId}. Marking as failed.", campaign.Id);
                    campaign.Status = CampaignStatus.Failed;
                    campaign.LastUpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }

                var completed = await ProcessBatchAsync(dbContext, campaign, session, stoppingToken);

                if (!completed)
                {
                    await Task.Delay(ActivePollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in campaign sender loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(
        ApplicationDbContext dbContext,
        Campaign campaign,
        CampaignSendSession session,
        CancellationToken cancellationToken)
    {
        var pendingRecipients = await dbContext.Recipients
            .Where(r => !dbContext.CampaignSendLogs.Any(l => l.CampaignId == campaign.Id && l.RecipientId == r.Id))
            .OrderBy(r => r.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingRecipients.Count == 0)
        {
            await FinalizeCampaignAsync(dbContext, campaign, cancellationToken);
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
                await emailSender.SendAsync(campaign, recipient, session.Settings, cancellationToken);
            }
            catch (Exception ex)
            {
                log.Status = CampaignSendStatus.Failed;
                log.ErrorMessage = TruncateError(ex.Message);
                logger.LogError(ex, "Failed to send campaign {CampaignId} to {RecipientEmail}", campaign.Id, recipient.Email);
            }

            logs.Add(log);
        }

        await dbContext.CampaignSendLogs.AddRangeAsync(logs, cancellationToken);
        campaign.LastUpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return false;
    }

    private static async Task FinalizeCampaignAsync(
        ApplicationDbContext dbContext,
        Campaign campaign,
        CancellationToken cancellationToken)
    {
        var sent = await dbContext.CampaignSendLogs
            .Where(l => l.CampaignId == campaign.Id && l.Status == CampaignSendStatus.Sent)
            .CountAsync(cancellationToken);
        var failed = await dbContext.CampaignSendLogs
            .Where(l => l.CampaignId == campaign.Id && l.Status == CampaignSendStatus.Failed)
            .CountAsync(cancellationToken);

        campaign.Status = failed > 0 ? CampaignStatus.Failed : CampaignStatus.Completed;
        campaign.LastUpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
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

