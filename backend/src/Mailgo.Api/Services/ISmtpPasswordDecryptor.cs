// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Mailgo.Api.Services;

public interface ISmtpPasswordDecryptor
{
    string KeyId { get; }
    string PublicKeyPem { get; }

    Task<string?> DecryptAsync(string? cipherTextBase64, string? keyId, CancellationToken cancellationToken = default);
}
