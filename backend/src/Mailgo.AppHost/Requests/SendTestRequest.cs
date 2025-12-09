// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mailgo.Api.Requests;

public class SendTestRequest : SmtpSettingsRequest
{
    [Required, EmailAddress]
    [JsonPropertyName("testEmail")]
    public string TestEmail { get; set; } = string.Empty;
}

