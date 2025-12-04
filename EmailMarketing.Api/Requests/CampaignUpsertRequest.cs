using System.ComponentModel.DataAnnotations;

namespace EmailMarketing.Api.Requests;

public class CampaignUpsertRequest
{
    [Required, MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string FromName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string FromEmail { get; set; } = string.Empty;

    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    public string? TextBody { get; set; }
}
