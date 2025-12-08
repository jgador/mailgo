// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Mailgo.Api.Responses;

public class SmtpPublicKeyResponse
{
    public SmtpPublicKeyResponse(string keyId, string publicKeyPem)
    {
        KeyId = keyId;
        PublicKeyPem = publicKeyPem;
    }

    [JsonPropertyName("keyId")]
    public string KeyId { get; }

    [JsonPropertyName("publicKeyPem")]
    public string PublicKeyPem { get; }
}
