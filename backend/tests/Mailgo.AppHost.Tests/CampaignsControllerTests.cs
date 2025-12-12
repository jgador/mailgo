// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

#pragma warning disable CA2007
// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mailgo.Api.Enums;
using Mailgo.Api.Requests;
using Mailgo.Api.Responses;
using Mailgo.Domain.Entities;
using Mailgo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mailgo.AppHost.Tests;

public class CampaignsControllerTests : IClassFixture<MailgoApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly MailgoApiFactory _factory;
    private readonly HttpClient _client;

    public CampaignsControllerTests(MailgoApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCampaign_and_get_detail_round_trips()
    {
        await _factory.ResetDatabaseAsync();

        var request = new CampaignUpsertRequest
        {
            Name = "Launch",
            Subject = "Hello",
            FromName = "Marketing",
            HtmlBody = "<p>Body</p>"
        };

        var createResponse = await _client.PostAsJsonAsync("api/campaigns", request, JsonOptions);

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(request.Name, created!.Name);
        Assert.Equal(CampaignStatus.Draft, created.Status);

        var fetched = await _client.GetFromJsonAsync<CampaignDetailResponse>($"api/campaigns/{created.Id}", JsonOptions);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(request.Subject, fetched.Subject);
    }

    [Fact]
    public async Task SendTest_returns_no_content_and_invokes_email_sender()
    {
        await _factory.ResetDatabaseAsync();
        _factory.EmailSender.Reset();

        var campaign = await CreateCampaignAsync();

        var request = new SendTestRequest
        {
            TestEmail = "preview@example.com",
            SmtpHost = "smtp.test",
            SmtpPort = 2525,
            Encryption = EncryptionType.None,
            SmtpPassword = "pw"
        };

        var response = await _client.PostAsJsonAsync($"api/campaigns/{campaign.Id}/send-test", request, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var call = Assert.Single(_factory.EmailSender.Calls);
        Assert.Equal(campaign.Id, call.Campaign.Id);
        Assert.Equal(request.TestEmail, call.OverrideRecipientEmail);
        Assert.Equal(request.SmtpHost, call.Settings.Host);
    }

    [Fact]
    public async Task SendNow_sets_campaign_to_sending_when_recipients_exist()
    {
        await _factory.ResetDatabaseAsync();
        _factory.EmailSender.Reset();

        var recipient = new Recipient
        {
            Email = "recipient@example.com",
            CreatedAt = DateTime.UtcNow
        };
        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Recipients.Add(recipient);
            await db.SaveChangesAsync();
        });

        var campaign = await CreateCampaignAsync();

        var request = new SendNowRequest
        {
            SmtpHost = "smtp.test",
            SmtpPort = 2525,
            Encryption = EncryptionType.StartTls,
            SmtpPassword = "pw"
        };

        var response = await _client.PostAsJsonAsync($"api/campaigns/{campaign.Id}/send-now", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var persisted = await _factory.ExecuteDbContextAsync(db => db.Campaigns.AsNoTracking().FirstAsync(c => c.Id == campaign.Id));
        Assert.Equal(CampaignStatus.Sending, persisted.Status);
        Assert.Equal(1, persisted.TargetRecipientCount);
    }

    private async Task<CampaignDetailResponse> CreateCampaignAsync()
    {
        var request = new CampaignUpsertRequest
        {
            Name = "Test Campaign",
            Subject = "Test Subject",
            FromName = "Sender",
            HtmlBody = "<p>Test</p>"
        };

        var createResponse = await _client.PostAsJsonAsync("api/campaigns", request, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CampaignDetailResponse>(JsonOptions);
        return created!;
    }
}
#pragma warning restore CA2007
