// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mailgo.Api.Options;
using Microsoft.Extensions.Options;

namespace Mailgo.Api.Services;

public class RsaSmtpPasswordDecryptor : ISmtpPasswordDecryptor, IDisposable
{
    private readonly Lazy<RSA> rsa;

    public string KeyId { get; }

    public string PublicKeyPem { get; }

    public RsaSmtpPasswordDecryptor(IOptions<SmtpEncryptionKeyOptions> options)
    {
        var value = options.Value ?? throw new ArgumentNullException(nameof(options));
        KeyId = value.KeyId ?? string.Empty;
        PublicKeyPem = value.PublicKeyPem ?? string.Empty;

        rsa = new Lazy<RSA>(() =>
        {
            var instance = RSA.Create();
            instance.ImportFromPem(value.PrivateKeyPem);
            return instance;
        });
    }

    public Task<string?> DecryptAsync(string? cipherTextBase64, string? keyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cipherTextBase64))
        {
            return Task.FromResult<string?>(null);
        }

        if (!string.Equals(keyId, KeyId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Password encryption key mismatch.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var cipherBytes = Convert.FromBase64String(cipherTextBase64);
            var plainBytes = rsa.Value.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
            var plaintext = Encoding.UTF8.GetString(plainBytes);
            return Task.FromResult<string?>(plaintext);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Invalid encrypted password payload.", ex);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Unable to decrypt SMTP password.", ex);
        }
    }

    public void Dispose()
    {
        if (rsa.IsValueCreated)
        {
            rsa.Value.Dispose();
        }
    }
}
