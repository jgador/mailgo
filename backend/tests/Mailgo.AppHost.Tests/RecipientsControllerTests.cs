// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Mailgo.Api.Responses;
using Xunit;

namespace Mailgo.AppHost.Tests;

public class RecipientsControllerTests : IClassFixture<MailgoApiFactory>
{
    private readonly MailgoApiFactory _factory;
    private readonly HttpClient _client;

    public RecipientsControllerTests(MailgoApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadRecipients_accepts_csv_and_lists_recipients()
    {
        await _factory.ResetDatabaseAsync();

        var csv = new StringBuilder()
            .AppendLine("email,first_name,last_name")
            .AppendLine("alice@example.com,Alice,Smith")
            .AppendLine("invalid-email,Bad,Row")
            .AppendLine("alice@example.com,Duplicate,Row")
            .AppendLine("bob@example.com,Bob,Stone")
            .ToString();

        using var multipart = new MultipartFormDataContent();
        using var fileContent = new StringContent(csv, Encoding.UTF8, "text/csv");
        multipart.Add(fileContent, "file", "recipients.csv");

        var response = await _client.PostAsync("api/recipients/upload", multipart);

        response.EnsureSuccessStatusCode();
        var uploadResult = await response.Content.ReadFromJsonAsync<RecipientUploadResultResponse>();
        Assert.NotNull(uploadResult);
        Assert.Equal(4, uploadResult!.TotalRows);
        Assert.Equal(2, uploadResult.Inserted);
        Assert.Equal(2, uploadResult.SkippedInvalid);

        var recipientsPage = await _client.GetFromJsonAsync<PagedResult<RecipientResponse>>("api/recipients?page=1&pageSize=10");

        Assert.NotNull(recipientsPage);
        Assert.Equal(2, recipientsPage!.TotalItems);
        Assert.Contains(recipientsPage.Items, r => r.Email == "alice@example.com");
        Assert.Contains(recipientsPage.Items, r => r.Email == "bob@example.com");
    }

    [Fact]
    public async Task UploadRecipients_returns_bad_request_when_file_missing()
    {
        await _factory.ResetDatabaseAsync();

        using var multipart = new MultipartFormDataContent();
        var response = await _client.PostAsync("api/recipients/upload", multipart);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
