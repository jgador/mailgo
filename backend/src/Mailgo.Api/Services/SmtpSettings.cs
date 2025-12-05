using Mailgo.Api.Enums;

namespace Mailgo.Api.Services;

public record SmtpSettings(
    string Host,
    int Port,
    string? Username,
    string? Password,
    EncryptionType Encryption,
    string? EncryptionHostname,
    bool AllowSelfSignedCertificates,
    string? OverrideFromName,
    string? OverrideFromAddress);

