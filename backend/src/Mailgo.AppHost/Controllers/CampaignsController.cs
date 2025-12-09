// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mailgo.Api.Requests;
using Mailgo.Api.Responses;
using Mailgo.Api.Services;
using Mailgo.Api.Stores;
using Mailgo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Mailgo.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
public class CampaignsController(
    CampaignStore campaignStore,
    ICampaignSendSessionStore sessionStore,
    IEmailSender emailSender,
    ISmtpPasswordDecryptor passwordDecryptor,
    ILogger<CampaignsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CampaignSummaryResponse>>> GetCampaigns(CancellationToken cancellationToken)
    {
        var response = await campaignStore.GetSummariesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignDetailResponse>> GetCampaign(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await campaignStore.GetDetailAsync(id, cancellationToken).ConfigureAwait(false);
        if (campaign is null)
        {
            return NotFound();
        }

        return Ok(campaign);
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<IEnumerable<CampaignSendLogResponse>>> GetLogs(Guid id, CancellationToken cancellationToken)
    {
        var logs = await campaignStore.GetLogsAsync(id, cancellationToken).ConfigureAwait(false);
        if (logs is null)
        {
            return NotFound();
        }

        return Ok(logs);
    }

    [HttpPost]
    public async Task<ActionResult<CampaignDetailResponse>> CreateCampaign(
        CampaignUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var createdCampaign = await campaignStore.CreateCampaignAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetCampaign), new { id = createdCampaign.Id }, createdCampaign);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CampaignDetailResponse>> UpdateCampaign(
        Guid id,
        CampaignUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await campaignStore.UpdateCampaignAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.ErrorMessage is not null)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Response);
    }

    [HttpPost("{id:guid}/send-test")]
    public async Task<IActionResult> SendTest(
        Guid id,
        SendTestRequest request,
        CancellationToken cancellationToken)
    {
        var campaign = await campaignStore.GetCampaignAsync(id, cancellationToken).ConfigureAwait(false);
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
            var smtpSettings = await request.ToSettingsAsync(passwordDecryptor, cancellationToken).ConfigureAwait(false);

            await emailSender.SendAsync(campaign, tempRecipient, smtpSettings, cancellationToken, request.TestEmail)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid SMTP password payload for campaign {CampaignId}", campaign.Id);
            return BadRequest("Invalid SMTP password payload.");
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
        var result = await campaignStore.PrepareSendNowAsync(id, cancellationToken).ConfigureAwait(false);
        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.ErrorMessage is not null)
        {
            return BadRequest(result.ErrorMessage);
        }

        var campaign = result.Campaign!;
        try
        {
            var smtpSettings = await request.ToSettingsAsync(passwordDecryptor, cancellationToken).ConfigureAwait(false);
            sessionStore.Upsert(new CampaignSendSession(campaign.Id, smtpSettings, DateTime.UtcNow));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid SMTP password payload for campaign {CampaignId}", campaign.Id);
            return BadRequest("Invalid SMTP password payload.");
        }

        logger.LogInformation(
            "Campaign {CampaignId} queued for sending to {RecipientCount} recipients",
            campaign.Id,
            result.RecipientCount);

        return Accepted();
    }
}

