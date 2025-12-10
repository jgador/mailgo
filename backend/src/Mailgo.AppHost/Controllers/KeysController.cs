// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Mailgo.Api.Responses;
using Mailgo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mailgo.Api.Controllers;

[ApiController]
[Route("api/keys")]
public class KeysController : ControllerBase
{
    private readonly ISmtpPasswordDecryptor _decryptor;

    public KeysController(ISmtpPasswordDecryptor decryptor)
    {
        _decryptor = decryptor;
    }

    [HttpGet("smtp")]
    public ActionResult<SmtpPublicKeyResponse> GetSmtpKey()
    {
        var response = new SmtpPublicKeyResponse(_decryptor.KeyId, _decryptor.PublicKeyPem);
        return Ok(response);
    }
}
