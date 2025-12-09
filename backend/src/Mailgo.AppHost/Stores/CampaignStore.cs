// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mailgo.Api.Data;
using Mailgo.Api.Requests;
using Mailgo.Api.Responses;
using Mailgo.Domain.Entities;
using Mailgo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Mailgo.Api.Stores;

public class CampaignStore
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public CampaignStore(IDbContextFactory<ApplicationDbContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<IReadOnlyCollection<CampaignSummaryResponse>> GetSummariesAsync(CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var campaigns = await dbContext.Campaigns
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (campaigns.Count == 0)
        {
            return Array.Empty<CampaignSummaryResponse>();
        }

        var campaignIds = campaigns.Select(c => c.Id).ToList();

        var statsLookup = await dbContext.CampaignSendLogs
            .AsNoTracking()
            .Where(l => campaignIds.Contains(l.CampaignId))
            .GroupBy(l => l.CampaignId)
            .Select(g => new
            {
                CampaignId = g.Key,
                Sent = g.Count(l => l.Status == CampaignSendStatus.Sent),
                Failed = g.Count(l => l.Status == CampaignSendStatus.Failed)
            })
            .ToDictionaryAsync(x => x.CampaignId, cancellationToken)
            .ConfigureAwait(false);

        var fallbackRecipientCount = await dbContext.Recipients.CountAsync(cancellationToken).ConfigureAwait(false);

        return campaigns
            .Select(c =>
            {
                statsLookup.TryGetValue(c.Id, out var stats);
                var sent = stats?.Sent ?? 0;
                var failed = stats?.Failed ?? 0;
                var total = ResolveTotalRecipients(c, sent, failed, fallbackRecipientCount);
                return c.ToSummaryResponse(total, sent, failed);
            })
            .ToList();
    }

    public async Task<CampaignDetailResponse?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var campaign = await dbContext.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (campaign is null)
        {
            return null;
        }

        var (sent, failed) = await GetSendStatsAsync(dbContext, id, cancellationToken).ConfigureAwait(false);
        var total = await ResolveDetailTotalAsync(dbContext, campaign, sent, failed, cancellationToken).ConfigureAwait(false);

        return campaign.ToDetailResponse(total, sent, failed);
    }

    public async Task<IReadOnlyCollection<CampaignSendLogResponse>?> GetLogsAsync(Guid id, CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var exists = await dbContext.Campaigns
            .AsNoTracking()
            .AnyAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return null;
        }

        var logs = await dbContext.CampaignSendLogs
            .AsNoTracking()
            .Include(l => l.Recipient)
            .Where(l => l.CampaignId == id)
            .OrderByDescending(l => l.SentAt ?? DateTime.MinValue)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return logs.Select(l => l.ToResponse()).ToList();
    }

    public async Task<CampaignDetailResponse> CreateCampaignAsync(CampaignUpsertRequest request, CancellationToken cancellationToken)
    {
        var campaign = new Campaign
        {
            Name = request.Name.Trim(),
            Subject = request.Subject.Trim(),
            FromName = request.FromName.Trim(),
            FromEmail = request.FromEmail.Trim(),
            HtmlBody = request.HtmlBody,
            Status = CampaignStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Campaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var recipients = await dbContext.Recipients.CountAsync(cancellationToken).ConfigureAwait(false);
        return campaign.ToDetailResponse(recipients, 0, 0);
    }

    public async Task<UpdateCampaignResult> UpdateCampaignAsync(Guid id, CampaignUpsertRequest request, CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var campaign = await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null)
        {
            return UpdateCampaignResult.CreateNotFound();
        }

        if (campaign.Status != CampaignStatus.Draft)
        {
            return UpdateCampaignResult.CreateInvalid("Only draft campaigns can be edited.");
        }

        campaign.Name = request.Name.Trim();
        campaign.Subject = request.Subject.Trim();
        campaign.FromName = request.FromName.Trim();
        campaign.FromEmail = request.FromEmail.Trim();
        campaign.HtmlBody = request.HtmlBody;
        campaign.LastUpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var (sent, failed) = await GetSendStatsAsync(dbContext, id, cancellationToken).ConfigureAwait(false);
        var total = await ResolveDetailTotalAsync(dbContext, campaign, sent, failed, cancellationToken).ConfigureAwait(false);

        return UpdateCampaignResult.CreateSuccess(campaign.ToDetailResponse(total, sent, failed));
    }

    public async Task<Campaign?> GetCampaignAsync(Guid id, CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<SendNowPreparationResult> PrepareSendNowAsync(Guid id, CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var campaign = await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null)
        {
            return SendNowPreparationResult.CreateNotFound();
        }

        if (campaign.Status != CampaignStatus.Draft)
        {
            return SendNowPreparationResult.CreateInvalid("Only draft campaigns can be sent.");
        }

        var recipientCount = await dbContext.Recipients.CountAsync(cancellationToken).ConfigureAwait(false);
        if (recipientCount == 0)
        {
            return SendNowPreparationResult.CreateInvalid("Upload recipients before sending.");
        }

        campaign.Status = CampaignStatus.Sending;
        campaign.TargetRecipientCount = recipientCount;
        campaign.LastUpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return SendNowPreparationResult.CreateSuccess(campaign, recipientCount);
    }

    public async Task<Campaign?> GetNextSendingCampaignAsync(CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.Campaigns
            .AsNoTracking()
            .Where(c => c.Status == CampaignStatus.Sending)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MarkCampaignAsFailedAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var campaign = await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null)
        {
            return;
        }

        campaign.Status = CampaignStatus.Failed;
        campaign.LastUpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Recipient>> GetPendingRecipientsAsync(
        Guid campaignId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.Recipients
            .AsNoTracking()
            .Where(r => !dbContext.CampaignSendLogs.Any(l => l.CampaignId == campaignId && l.RecipientId == r.Id))
            .OrderBy(r => r.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddSendLogsAsync(
        Guid campaignId,
        IEnumerable<CampaignSendLog> logs,
        CancellationToken cancellationToken)
    {
        var logList = logs.ToList();
        if (logList.Count == 0)
        {
            return;
        }

        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await dbContext.CampaignSendLogs.AddRangeAsync(logList, cancellationToken).ConfigureAwait(false);

        var campaign = await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            .ConfigureAwait(false);
        campaign?.LastUpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task FinalizeCampaignAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var campaign = await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null)
        {
            return;
        }

        var (sent, failed) = await GetSendStatsAsync(dbContext, campaignId, cancellationToken).ConfigureAwait(false);
        campaign.Status = failed > 0 ? CampaignStatus.Failed : CampaignStatus.Completed;
        campaign.LastUpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static int ResolveTotalRecipients(Campaign campaign, int sent, int failed, int fallbackRecipientCount)
    {
        if (campaign.TargetRecipientCount > 0)
        {
            return campaign.TargetRecipientCount;
        }

        var totalFromLogs = sent + failed;
        if (totalFromLogs > 0)
        {
            return totalFromLogs;
        }

        return fallbackRecipientCount;
    }

    private static async Task<int> ResolveDetailTotalAsync(
        ApplicationDbContext dbContext,
        Campaign campaign,
        int sent,
        int failed,
        CancellationToken cancellationToken)
    {
        if (campaign.TargetRecipientCount > 0)
        {
            return campaign.TargetRecipientCount;
        }

        var totalFromLogs = sent + failed;
        if (totalFromLogs > 0)
        {
            return totalFromLogs;
        }

        return await dbContext.Recipients.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(int sent, int failed)> GetSendStatsAsync(
        ApplicationDbContext dbContext,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var sent = await dbContext.CampaignSendLogs
            .AsNoTracking()
            .Where(l => l.CampaignId == campaignId && l.Status == CampaignSendStatus.Sent)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var failed = await dbContext.CampaignSendLogs
            .AsNoTracking()
            .Where(l => l.CampaignId == campaignId && l.Status == CampaignSendStatus.Failed)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        return (sent, failed);
    }
}

public record UpdateCampaignResult(CampaignDetailResponse? Response, bool NotFound, string? ErrorMessage)
{
    public static UpdateCampaignResult CreateNotFound() => new(null, true, null);

    public static UpdateCampaignResult CreateInvalid(string message) => new(null, false, message);

    public static UpdateCampaignResult CreateSuccess(CampaignDetailResponse response) => new(response, false, null);
}

public record SendNowPreparationResult(Campaign? Campaign, int RecipientCount, bool NotFound, string? ErrorMessage)
{
    public static SendNowPreparationResult CreateNotFound() => new(null, 0, true, null);

    public static SendNowPreparationResult CreateInvalid(string message) => new(null, 0, false, message);

    public static SendNowPreparationResult CreateSuccess(Campaign campaign, int recipientCount) =>
        new(campaign, recipientCount, false, null);
}
