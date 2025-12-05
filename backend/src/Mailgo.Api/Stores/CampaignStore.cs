// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Mailgo.Api.Data;
using Mailgo.Api.Requests;
using Mailgo.Api.Responses;
using Mailgo.Domain.Entities;
using Mailgo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Mailgo.Api.Stores;

public class CampaignStore
{
    private readonly ApplicationDbContext _dbContext;

    public CampaignStore(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyCollection<CampaignSummaryResponse>> GetSummariesAsync(CancellationToken cancellationToken)
    {
        var campaigns = await _dbContext.Campaigns
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (campaigns.Count == 0)
        {
            return Array.Empty<CampaignSummaryResponse>();
        }

        var campaignIds = campaigns.Select(c => c.Id).ToList();

        var statsLookup = await _dbContext.CampaignSendLogs
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

        var fallbackRecipientCount = await _dbContext.Recipients.CountAsync(cancellationToken).ConfigureAwait(false);

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
        var campaign = await _dbContext.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (campaign is null)
        {
            return null;
        }

        var (sent, failed) = await GetSendStatsAsync(id, cancellationToken).ConfigureAwait(false);
        var total = await ResolveDetailTotalAsync(campaign, sent, failed, cancellationToken).ConfigureAwait(false);

        return campaign.ToDetailResponse(total, sent, failed);
    }

    public async Task<IReadOnlyCollection<CampaignSendLogResponse>?> GetLogsAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Campaigns.AnyAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return null;
        }

        var logs = await _dbContext.CampaignSendLogs
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
            TextBody = request.TextBody,
            Status = CampaignStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _dbContext.Campaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var recipients = await _dbContext.Recipients.CountAsync(cancellationToken).ConfigureAwait(false);
        return campaign.ToDetailResponse(recipients, 0, 0);
    }

    public async Task<UpdateCampaignResult> UpdateCampaignAsync(Guid id, CampaignUpsertRequest request, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
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
        campaign.TextBody = request.TextBody;
        campaign.LastUpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var (sent, failed) = await GetSendStatsAsync(id, cancellationToken).ConfigureAwait(false);
        var total = await ResolveDetailTotalAsync(campaign, sent, failed, cancellationToken).ConfigureAwait(false);

        return UpdateCampaignResult.CreateSuccess(campaign.ToDetailResponse(total, sent, failed));
    }

    public async Task<Campaign?> GetCampaignAsync(Guid id, CancellationToken cancellationToken) =>
        await _dbContext.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<SendNowPreparationResult> PrepareSendNowAsync(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null)
        {
            return SendNowPreparationResult.CreateNotFound();
        }

        if (campaign.Status != CampaignStatus.Draft)
        {
            return SendNowPreparationResult.CreateInvalid("Only draft campaigns can be sent.");
        }

        var recipientCount = await _dbContext.Recipients.CountAsync(cancellationToken).ConfigureAwait(false);
        if (recipientCount == 0)
        {
            return SendNowPreparationResult.CreateInvalid("Upload recipients before sending.");
        }

        campaign.Status = CampaignStatus.Sending;
        campaign.TargetRecipientCount = recipientCount;
        campaign.LastUpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return SendNowPreparationResult.CreateSuccess(campaign, recipientCount);
    }

    public async Task<Campaign?> GetNextSendingCampaignAsync(CancellationToken cancellationToken) =>
        await _dbContext.Campaigns
            .Where(c => c.Status == CampaignStatus.Sending)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task MarkCampaignAsFailedAsync(Campaign campaign, CancellationToken cancellationToken)
    {
        campaign.Status = CampaignStatus.Failed;
        campaign.LastUpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Recipient>> GetPendingRecipientsAsync(
        Guid campaignId,
        int batchSize,
        CancellationToken cancellationToken) =>
        await _dbContext.Recipients
            .Where(r => !_dbContext.CampaignSendLogs.Any(l => l.CampaignId == campaignId && l.RecipientId == r.Id))
            .OrderBy(r => r.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddSendLogsAsync(IEnumerable<CampaignSendLog> logs, CancellationToken cancellationToken)
    {
        await _dbContext.CampaignSendLogs.AddRangeAsync(logs, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    public async Task FinalizeCampaignAsync(Campaign campaign, CancellationToken cancellationToken)
    {
        var (sent, failed) = await GetSendStatsAsync(campaign.Id, cancellationToken).ConfigureAwait(false);
        campaign.Status = failed > 0 ? CampaignStatus.Failed : CampaignStatus.Completed;
        campaign.LastUpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    private async Task<int> ResolveDetailTotalAsync(Campaign campaign, int sent, int failed, CancellationToken cancellationToken)
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

        return await _dbContext.Recipients.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<(int sent, int failed)> GetSendStatsAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var sent = await _dbContext.CampaignSendLogs
            .AsNoTracking()
            .Where(l => l.CampaignId == campaignId && l.Status == CampaignSendStatus.Sent)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var failed = await _dbContext.CampaignSendLogs
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
