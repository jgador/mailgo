// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mailgo.Api.Requests;

public class CampaignUpsertRequest : IValidatableObject
{
    [Required, MaxLength(256)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    [JsonPropertyName("fromName")]
    public string FromName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    [JsonPropertyName("fromEmail")]
    public string FromEmail { get; set; } = string.Empty;

    [JsonPropertyName("htmlBody")]
    public string HtmlBody { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(HtmlBody))
        {
            yield return new ValidationResult(
                "HTML Body is required.",
                new[] { nameof(HtmlBody) });
        }
    }
}

