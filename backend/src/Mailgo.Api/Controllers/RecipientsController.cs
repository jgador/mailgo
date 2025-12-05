// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net.Mail;
using CsvHelper;
using CsvHelper.Configuration;
using Mailgo.Api.Responses;
using Mailgo.Api.Stores;
using Mailgo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Mailgo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipientsController(RecipientStore recipientStore) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<RecipientResponse>>> GetRecipients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var recipientsPage = await recipientStore.GetRecipientsAsync(page, pageSize, cancellationToken).ConfigureAwait(false);
        return Ok(recipientsPage);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<RecipientUploadResultResponse>> UploadRecipients(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("CSV file is required.");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant() ?? string.Empty,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim | TrimOptions.InsideQuotes
        };

        var totalRows = 0;
        var insertedCount = 0;
        var skippedCount = 0;

        var existingEmails = await recipientStore.GetExistingRecipientEmailsAsync(cancellationToken).ConfigureAwait(false);

        var newRecipients = new List<Recipient>();

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        try
        {
            if (config.HasHeaderRecord)
            {
                if (!await csv.ReadAsync().ConfigureAwait(false))
                {
                    return BadRequest("CSV file is empty.");
                }

                csv.ReadHeader();
            }

            while (await csv.ReadAsync().ConfigureAwait(false))
            {
                totalRows++;
                var email = csv.GetField("email");

                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                {
                    skippedCount++;
                    continue;
                }

                var normalizedEmail = email.Trim();
                var dedupeKey = normalizedEmail.ToLowerInvariant();

                if (!existingEmails.Add(dedupeKey))
                {
                    skippedCount++;
                    continue;
                }

                var recipient = new Recipient
                {
                    Email = normalizedEmail,
                    FirstName = csv.TryGetField("first_name", out string? firstName) ? firstName : null,
                    LastName = csv.TryGetField("last_name", out string? lastName) ? lastName : null,
                    CreatedAt = DateTime.UtcNow
                };

                newRecipients.Add(recipient);
            }
        }
        catch (HeaderValidationException)
        {
            return BadRequest("CSV must contain at least an 'email' column.");
        }

        insertedCount = await recipientStore.SaveRecipientsAsync(newRecipients, cancellationToken).ConfigureAwait(false);

        var uploadResult = new RecipientUploadResultResponse(totalRows, insertedCount, skippedCount);
        return Ok(uploadResult);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

