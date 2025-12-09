// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Mailgo.Api.Responses;
using Mailgo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mailgo.Api.Controllers;

[ApiController]
[Route("api/keys")]
public class KeysController(ISmtpPasswordDecryptor decryptor) : ControllerBase
{
    [HttpGet("smtp")]
    public ActionResult<SmtpPublicKeyResponse> GetSmtpKey()
    {
        var response = new SmtpPublicKeyResponse(decryptor.KeyId, decryptor.PublicKeyPem);
        return Ok(response);
    }
}
