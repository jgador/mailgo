using System.Globalization;
using System.Net.Mail;
using CsvHelper;
using CsvHelper.Configuration;
using Mailgo.Api.Data;
using Mailgo.Api.Responses;
using Mailgo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mailgo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipientsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<RecipientResponse>>> GetRecipients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = dbContext.Recipients.AsNoTracking().OrderByDescending(r => r.CreatedAt);
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dto = new PagedResult<RecipientResponse>(
            items.Select(r => r.ToResponse()).ToList(),
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize));

        return Ok(dto);
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
        var inserted = 0;
        var skipped = 0;

        var existingEmails = new HashSet<string>(
            await dbContext.Recipients
                .AsNoTracking()
                .Select(r => r.Email.ToLowerInvariant())
                .ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var newRecipients = new List<Recipient>();

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        try
        {
            if (config.HasHeaderRecord)
            {
                if (!await csv.ReadAsync())
                {
                    return BadRequest("CSV file is empty.");
                }

                csv.ReadHeader();
            }

            while (await csv.ReadAsync())
            {
                totalRows++;
                var email = csv.GetField("email");

                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                {
                    skipped++;
                    continue;
                }

                var normalizedEmail = email.Trim();
                var dedupeKey = normalizedEmail.ToLowerInvariant();

                if (!existingEmails.Add(dedupeKey))
                {
                    skipped++;
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

        if (newRecipients.Count > 0)
        {
            await dbContext.Recipients.AddRangeAsync(newRecipients, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            inserted = newRecipients.Count;
        }

        var result = new RecipientUploadResultResponse(totalRows, inserted, skipped);
        return Ok(result);
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

