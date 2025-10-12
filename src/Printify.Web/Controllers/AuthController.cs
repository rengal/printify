using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Auth.GetCurrentUser;
using Printify.Application.Interfaces;
using Printify.Infrastructure.Config;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Users.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IOptions<JwtOptions> jwtOptions, IMediator mediator, ISessionRepository sessionService, IJwtTokenGenerator jwt) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(
        [FromBody]LoginRequestDto request,
        CancellationToken cancellationToken,
        [FromServices] IJwtTokenGenerator jwt)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DisplayName);

        var command = request.ToCommand(HttpContext.CaptureRequestContext());
        var user = await mediator.Send(command, cancellationToken);

        var token = jwt.GenerateToken(userId: user.Id, null);
        var responseDto = new LoginResponseDto(AccessToken: token,
            TokenType: "Bearer",
            ExpiresInSeconds: jwtOptions.Value.ExpiresInSeconds,
            User: user.ToDto());

        return Ok(responseDto);
    }

    [HttpPost("anonymous")]
    public async Task<ActionResult> CreateAnonymousSession(CancellationToken cancellationToken)
    {
        // var command = new CreateAnonymousSessionCommand();
        // await mediator.Send(command, cancellationToken);

        return Ok(); //todo JWT token for anonymous session
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var command = new GetCurrentUserCommand(HttpContext.CaptureRequestContext());
        var user = await mediator.Send(command, cancellationToken);

        return Ok(user.ToDto());
    }
}
