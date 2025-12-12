// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Mailgo.Api.Options;
using Mailgo.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Mailgo.AppHost.Tests.Services;

public class RsaSmtpPasswordDecryptorTests
{
    private const string KeyId = "smtp-key";

    [Fact]
    public async Task DecryptAsync_returns_plaintext_for_matching_key()
    {
        using var rsa = RSA.Create(2048);
        using var decryptor = CreateDecryptor(rsa, KeyId);

        var cipherText = EncryptWithPublicKey(rsa, "super-secret-password");

        var plaintext = await decryptor.DecryptAsync(cipherText, KeyId);

        Assert.Equal("super-secret-password", plaintext);
    }

    [Fact]
    public async Task DecryptAsync_throws_for_key_mismatch()
    {
        using var rsa = RSA.Create(2048);
        using var decryptor = CreateDecryptor(rsa, KeyId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            decryptor.DecryptAsync("AQAB", "different-key"));

        Assert.Equal("Password encryption key mismatch.", exception.Message);
    }

    [Fact]
    public async Task DecryptAsync_returns_null_for_empty_payload()
    {
        using var rsa = RSA.Create(2048);
        using var decryptor = CreateDecryptor(rsa, KeyId);

        var nullResult = await decryptor.DecryptAsync(null, KeyId);
        var whitespaceResult = await decryptor.DecryptAsync("   ", KeyId);

        Assert.Null(nullResult);
        Assert.Null(whitespaceResult);
    }

    [Fact]
    public async Task DecryptAsync_wraps_format_errors()
    {
        using var rsa = RSA.Create(2048);
        using var decryptor = CreateDecryptor(rsa, KeyId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            decryptor.DecryptAsync("not-base64", KeyId));

        Assert.Contains("Invalid encrypted password payload.", exception.Message);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Fact]
    public async Task DecryptAsync_respects_cancellation()
    {
        using var rsa = RSA.Create(2048);
        using var decryptor = CreateDecryptor(rsa, KeyId);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            decryptor.DecryptAsync("AQAB", KeyId, cts.Token));
    }

    private static RsaSmtpPasswordDecryptor CreateDecryptor(RSA rsa, string keyId)
    {
        var options = Options.Create(new SmtpEncryptionKeyOptions
        {
            KeyId = keyId,
            PublicKeyPem = ExportPublicKeyPem(rsa),
            PrivateKeyPem = ExportPrivateKeyPem(rsa),
        });

        return new RsaSmtpPasswordDecryptor(options);
    }

    private static string EncryptWithPublicKey(RSA rsaWithPrivateKey, string plaintext)
    {
        using var publicOnly = RSA.Create();
        publicOnly.ImportFromPem(ExportPublicKeyPem(rsaWithPrivateKey));

        var cipherBytes = publicOnly.Encrypt(Encoding.UTF8.GetBytes(plaintext), RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(cipherBytes);
    }

    private static string ExportPublicKeyPem(RSA rsa)
    {
        return BuildPem("PUBLIC KEY", rsa.ExportSubjectPublicKeyInfo());
    }

    private static string ExportPrivateKeyPem(RSA rsa)
    {
        return BuildPem("PRIVATE KEY", rsa.ExportPkcs8PrivateKey());
    }

    private static string BuildPem(string label, byte[] data)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"-----BEGIN {label}-----");
        builder.AppendLine(Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine($"-----END {label}-----");
        return builder.ToString();
    }
}
