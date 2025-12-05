using System.ComponentModel.DataAnnotations;
using Mailgo.Api.Enums;
using Mailgo.Api.Services;

namespace Mailgo.Api.Requests;

public class SmtpSettingsRequest
{
    [Required]
    public string SmtpHost { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 587;

    public string? SmtpUsername { get; set; }

    public string? SmtpPassword { get; set; }

    [Required]
    public EncryptionType Encryption { get; set; } = EncryptionType.StartTls;

    public string? EncryptionHostname { get; set; }

    public bool AllowSelfSigned { get; set; }

    [MaxLength(256)]
    public string? OverrideFromName { get; set; }

    [EmailAddress, MaxLength(256)]
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

