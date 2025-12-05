using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mailgo.Api.Requests;

public class SendTestRequest : SmtpSettingsRequest
{
    [Required, EmailAddress]
    [JsonPropertyName("testEmail")]
    public string TestEmail { get; set; } = string.Empty;
}

