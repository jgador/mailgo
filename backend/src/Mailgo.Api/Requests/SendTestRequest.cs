using System.ComponentModel.DataAnnotations;

namespace Mailgo.Api.Requests;

public class SendTestRequest : SmtpSettingsRequest
{
    [Required, EmailAddress]
    public string TestEmail { get; set; } = string.Empty;
}

