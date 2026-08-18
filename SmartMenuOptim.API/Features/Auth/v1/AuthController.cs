using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMenuOptim.Application.Features.Auth.Commands;
using SmartMenuOptim.Application.Features.Auth.DTOs;

namespace SmartMenuOptim.API.Features.Auth.v1;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IMediator mediator) : ControllerBase
{
    // AllowAnonymous: this endpoint is the credential exchange itself — no identity exists yet to authorize.
    // See docs/06-Security/AUTHENTICATION_FRAMEWORK.md §4.
    [AllowAnonymous]
    [HttpPost("sign-in")]
    public async Task<ActionResult<SignInResultDTO>> SignInAsync([FromBody] SignInWithPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return Unauthorized(result.Error);

        return Ok(result.Value);
    }
}
