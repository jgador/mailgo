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
public class CampaignsController : ControllerBase
{
    private readonly CampaignStore _campaignStore;
    private readonly ICampaignSendSessionStore _sessionStore;
    private readonly IEmailSender _emailSender;
    private readonly ISmtpPasswordDecryptor _passwordDecryptor;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        CampaignStore campaignStore,
        ICampaignSendSessionStore sessionStore,
        IEmailSender emailSender,
        ISmtpPasswordDecryptor passwordDecryptor,
        ILogger<CampaignsController> logger)
    {
        _campaignStore = campaignStore;
        _sessionStore = sessionStore;
        _emailSender = emailSender;
        _passwordDecryptor = passwordDecryptor;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CampaignSummaryResponse>>> GetCampaigns(CancellationToken cancellationToken)
    {
        var response = await _campaignStore.GetSummariesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignDetailResponse>> GetCampaign(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await _campaignStore.GetDetailAsync(id, cancellationToken).ConfigureAwait(false);
        if (campaign is null)
        {
            return NotFound();
        }

        return Ok(campaign);
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<IEnumerable<CampaignSendLogResponse>>> GetLogs(Guid id, CancellationToken cancellationToken)
    {
        var logs = await _campaignStore.GetLogsAsync(id, cancellationToken).ConfigureAwait(false);
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
        var createdCampaign = await _campaignStore.CreateCampaignAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetCampaign), new { id = createdCampaign.Id }, createdCampaign);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CampaignDetailResponse>> UpdateCampaign(
        Guid id,
        CampaignUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _campaignStore.UpdateCampaignAsync(id, request, cancellationToken).ConfigureAwait(false);
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
        var campaign = await _campaignStore.GetCampaignAsync(id, cancellationToken).ConfigureAwait(false);
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
            var smtpSettings = await request.ToSettingsAsync(_passwordDecryptor, cancellationToken).ConfigureAwait(false);

            await _emailSender.SendAsync(campaign, tempRecipient, smtpSettings, cancellationToken, request.TestEmail)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid SMTP password payload for campaign {CampaignId}", campaign.Id);
            return BadRequest("Invalid SMTP password payload.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test send failed for campaign {CampaignId}", campaign.Id);
            return StatusCode(502, "SMTP send failed. Check credentials and try again.");
        }
    }

    [HttpPost("{id:guid}/send-now")]
    public async Task<IActionResult> SendNow(
        Guid id,
        SendNowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _campaignStore.PrepareSendNowAsync(id, cancellationToken).ConfigureAwait(false);
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
            var smtpSettings = await request.ToSettingsAsync(_passwordDecryptor, cancellationToken).ConfigureAwait(false);
            _sessionStore.Upsert(new CampaignSendSession(campaign.Id, smtpSettings, DateTime.UtcNow));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid SMTP password payload for campaign {CampaignId}", campaign.Id);
            return BadRequest("Invalid SMTP password payload.");
        }

        _logger.LogInformation(
            "Campaign {CampaignId} queued for sending to {RecipientCount} recipients",
            campaign.Id,
            result.RecipientCount);

        return Accepted();
    }
}

