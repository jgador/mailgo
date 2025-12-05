// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Mailgo.Api.Data;
using Mailgo.Api.Requests;
using Mailgo.Api.Responses;
using Mailgo.Api.Services;
using Mailgo.Domain.Entities;
using Mailgo.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mailgo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController(
    ApplicationDbContext dbContext,
    ICampaignSendSessionStore sessionStore,
    IEmailSender emailSender,
    ILogger<CampaignsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CampaignSummaryResponse>>> GetCampaigns(CancellationToken cancellationToken)
    {
        var campaigns = await dbContext.Campaigns
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

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
            .ToDictionaryAsync(x => x.CampaignId, cancellationToken);

        var recipientCount = await dbContext.Recipients.CountAsync(cancellationToken);

        var response = campaigns.Select(c =>
        {
            statsLookup.TryGetValue(c.Id, out var stats);
            var sent = stats?.Sent ?? 0;
            var failed = stats?.Failed ?? 0;
            var totalFromLogs = sent + failed;
            var total = c.TargetRecipientCount > 0
                ? c.TargetRecipientCount
                : (totalFromLogs > 0 ? totalFromLogs : recipientCount);

            return c.ToSummaryResponse(total, sent, failed);
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignDetailResponse>> GetCampaign(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (campaign is null)
        {
            return NotFound();
        }

        var sent = await dbContext.CampaignSendLogs
            .AsNoTracking()
            .Where(l => l.CampaignId == id && l.Status == CampaignSendStatus.Sent)
            .CountAsync(cancellationToken);

        var failed = await dbContext.CampaignSendLogs
            .AsNoTracking()
            .Where(l => l.CampaignId == id && l.Status == CampaignSendStatus.Failed)
            .CountAsync(cancellationToken);

        var total = campaign.TargetRecipientCount > 0
            ? campaign.TargetRecipientCount
            : (sent + failed > 0 ? sent + failed : await dbContext.Recipients.CountAsync(cancellationToken));

        return Ok(campaign.ToDetailResponse(total, sent, failed));
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<IEnumerable<CampaignSendLogResponse>>> GetLogs(Guid id, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Campaigns.AnyAsync(c => c.Id == id, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var logs = await dbContext.CampaignSendLogs
            .AsNoTracking()
            .Include(l => l.Recipient)
            .Where(l => l.CampaignId == id)
            .OrderByDescending(l => l.SentAt ?? DateTime.MinValue)
            .ToListAsync(cancellationToken);

        return Ok(logs.Select(l => l.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<CampaignDetailResponse>> CreateCampaign(
        CampaignUpsertRequest request,
        CancellationToken cancellationToken)
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

        dbContext.Campaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken);

        var recipients = await dbContext.Recipients.CountAsync(cancellationToken);
        var dto = campaign.ToDetailResponse(recipients, 0, 0);

        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CampaignDetailResponse>> UpdateCampaign(
        Guid id,
        CampaignUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var campaign = await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.Status != CampaignStatus.Draft)
        {
            return BadRequest("Only draft campaigns can be edited.");
        }

        campaign.Name = request.Name.Trim();
        campaign.Subject = request.Subject.Trim();
        campaign.FromName = request.FromName.Trim();
        campaign.FromEmail = request.FromEmail.Trim();
        campaign.HtmlBody = request.HtmlBody;
        campaign.TextBody = request.TextBody;
        campaign.LastUpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var sent = await dbContext.CampaignSendLogs
            .Where(l => l.CampaignId == id && l.Status == CampaignSendStatus.Sent)
            .CountAsync(cancellationToken);
        var failed = await dbContext.CampaignSendLogs
            .Where(l => l.CampaignId == id && l.Status == CampaignSendStatus.Failed)
            .CountAsync(cancellationToken);
        var total = campaign.TargetRecipientCount > 0
            ? campaign.TargetRecipientCount
            : (sent + failed > 0 ? sent + failed : await dbContext.Recipients.CountAsync(cancellationToken));

        return Ok(campaign.ToDetailResponse(total, sent, failed));
    }

    [HttpPost("{id:guid}/send-test")]
    public async Task<IActionResult> SendTest(
        Guid id,
        SendTestRequest request,
        CancellationToken cancellationToken)
    {
        var campaign = await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        var tempRecipient = new Recipient
        {
            Id = Guid.Empty,
            Email = request.TestEmail,
            FirstName = "Test",
            LastName = "Recipient",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await emailSender.SendAsync(campaign, tempRecipient, request.ToSettings(), cancellationToken, request.TestEmail);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test send failed for campaign {CampaignId}", campaign.Id);
            return StatusCode(502, "SMTP send failed. Check credentials and try again.");
        }
    }

    [HttpPost("{id:guid}/send-now")]
    public async Task<IActionResult> SendNow(
        Guid id,
        SendNowRequest request,
        CancellationToken cancellationToken)
    {
        var campaign = await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.Status != CampaignStatus.Draft)
        {
            return BadRequest("Only draft campaigns can be sent.");
        }

        var recipientCount = await dbContext.Recipients.CountAsync(cancellationToken);
        if (recipientCount == 0)
        {
            return BadRequest("Upload recipients before sending.");
        }

        campaign.Status = CampaignStatus.Sending;
        campaign.TargetRecipientCount = recipientCount;
        campaign.LastUpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        sessionStore.Upsert(new CampaignSendSession(campaign.Id, request.ToSettings(), DateTime.UtcNow));
        logger.LogInformation("Campaign {CampaignId} queued for sending to {RecipientCount} recipients", campaign.Id, recipientCount);

        return Accepted();
    }
}

