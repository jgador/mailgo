// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

namespace Mailgo.Api.Options;

public class SmtpEncryptionKeyOptions
{
    public const string SectionName = "EncryptionKeys:Smtp";

    public string KeyId { get; set; } = string.Empty;

    public string PublicKeyPem { get; set; } = string.Empty;

    public string PrivateKeyPem { get; set; } = string.Empty;
}
