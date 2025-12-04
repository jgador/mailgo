using EmailMarketing.Api.Enums;

namespace EmailMarketing.Api.Services;

public record SmtpSettings(
    string Host,
    int Port,
    string? Username,
    string? Password,
    EncryptionType Encryption);
