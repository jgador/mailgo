// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Mailgo.Api.Enums;
using Mailgo.Api.Services;

namespace Mailgo.Api.Requests;

public class SmtpSettingsRequest
{
    [Required]
    [JsonPropertyName("smtpHost")]
    public string SmtpHost { get; set; } = string.Empty;

    [Range(1, 65535)]
    [JsonPropertyName("smtpPort")]
    public int SmtpPort { get; set; } = 587;

    [JsonPropertyName("smtpUsername")]
    public string? SmtpUsername { get; set; }

    [JsonPropertyName("smtpPassword")]
    public string? SmtpPassword { get; set; }

    [JsonPropertyName("smtpPasswordEncrypted")]
    public string? SmtpPasswordEncrypted { get; set; }

    [JsonPropertyName("smtpPasswordKeyId")]
    public string? SmtpPasswordKeyId { get; set; }

    [Required]
    [JsonPropertyName("encryption")]
    public EncryptionType Encryption { get; set; } = EncryptionType.StartTls;

    [JsonPropertyName("encryptionHostname")]
    public string? EncryptionHostname { get; set; }

    [JsonPropertyName("allowSelfSigned")]
    public bool AllowSelfSigned { get; set; }

    [MaxLength(256)]
    [JsonPropertyName("overrideFromName")]
    public string? OverrideFromName { get; set; }

    public async Task<SmtpSettings> ToSettingsAsync(
        ISmtpPasswordDecryptor decryptor,
        CancellationToken cancellationToken = default)
    {
        var password = string.IsNullOrWhiteSpace(SmtpPassword) ? null : SmtpPassword;

        if (password is null && !string.IsNullOrWhiteSpace(SmtpPasswordEncrypted))
        {
            password = await decryptor.DecryptAsync(SmtpPasswordEncrypted, SmtpPasswordKeyId, cancellationToken)
                .ConfigureAwait(false);
        }

        var finalPassword = string.IsNullOrWhiteSpace(password) ? null : password;

        return new SmtpSettings(
            SmtpHost,
            SmtpPort,
            string.IsNullOrWhiteSpace(SmtpUsername) ? null : SmtpUsername,
            finalPassword,
            Encryption,
            string.IsNullOrWhiteSpace(EncryptionHostname) ? null : EncryptionHostname,
            AllowSelfSigned,
            string.IsNullOrWhiteSpace(OverrideFromName) ? null : OverrideFromName);
    }
}
