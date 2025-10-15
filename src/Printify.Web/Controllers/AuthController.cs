using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Auth.GetCurrentUser;
using Printify.Application.Interfaces;
using Printify.Domain.AnonymousSessions;
using Printify.Infrastructure.Config;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Users.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IOptions<JwtOptions> jwtOptions, IMediator mediator, IAnonymousSessionRepository sessionRepository, IJwtTokenGenerator jwt) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(
        [FromBody]LoginRequestDto request,
        [FromServices] IJwtTokenGenerator jwt,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DisplayName);

        var command = request.ToCommand(HttpContext.CaptureRequestContext());
        var user = await mediator.Send(command, ct);

        var token = jwt.GenerateToken(userId: user.Id, null);
        var responseDto = new LoginResponseDto(AccessToken: token,
            TokenType: "Bearer",
            ExpiresInSeconds: jwtOptions.Value.ExpiresInSeconds,
            User: user.ToDto());

        return Ok(responseDto);
    }

    [HttpPost("anonymous")]
    public async Task<ActionResult<AnonymousSessionDto>> CreateAnonymousSession(CancellationToken ct)
    {
        var now = DateTimeOffset.Now;
        var requestContext = HttpContext.CaptureRequestContext();
        var session = new AnonymousSession(Guid.NewGuid(), now, now, requestContext.IpAddress, null);
        await sessionRepository.AddAsync(session, ct);
        // var command = new CreateAnonymousSessionCommand();
        // await mediator.Send(command, cancellationToken);

        return Ok(); //todo JWT token for anonymous session
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken ct)
    {
        var command = new GetCurrentUserCommand(HttpContext.CaptureRequestContext());
        var user = await mediator.Send(command, ct);

        return Ok(user.ToDto());
    }
}
