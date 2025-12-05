using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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

    [EmailAddress, MaxLength(256)]
    [JsonPropertyName("overrideFromAddress")]
    public string? OverrideFromAddress { get; set; }

    public SmtpSettings ToSettings() =>
        new(
            SmtpHost,
            SmtpPort,
            string.IsNullOrWhiteSpace(SmtpUsername) ? null : SmtpUsername,
            string.IsNullOrWhiteSpace(SmtpPassword) ? null : SmtpPassword,
            Encryption,
            string.IsNullOrWhiteSpace(EncryptionHostname) ? null : EncryptionHostname,
            AllowSelfSigned,
            string.IsNullOrWhiteSpace(OverrideFromName) ? null : OverrideFromName,
            string.IsNullOrWhiteSpace(OverrideFromAddress) ? null : OverrideFromAddress);
}
