using System.ComponentModel.DataAnnotations;
using EmailMarketing.Api.Enums;
using EmailMarketing.Api.Services;

namespace EmailMarketing.Api.Requests;

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

    public SmtpSettings ToSettings() =>
        new(
            SmtpHost,
            SmtpPort,
            string.IsNullOrWhiteSpace(SmtpUsername) ? null : SmtpUsername,
            string.IsNullOrWhiteSpace(SmtpPassword) ? null : SmtpPassword,
            Encryption);
}
