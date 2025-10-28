using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Printify.Application.Interfaces;
using Printify.Application.Features.Auth.CreateAnonymousSession;
using Printify.Application.Features.Auth.GetCurrentUser;
using Printify.Infrastructure.Config;
using Printify.Web.Contracts.Auth.AnonymousSession.Response;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Users.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IOptions<JwtOptions> jwtOptions, IMediator mediator, IJwtTokenGenerator jwt) : ControllerBase
{
    // POST api/auth/login: authenticates a known user and issues an access token.
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody]LoginRequestDto request,
        CancellationToken ct)
    {
        if (request is null)
        {
            // Treat missing body as client error so tests can rely on consistent 400 surface area.
            return BadRequest("Request body is required.");
        }

        // Capture the caller context to keep session/user correlation consistent.
        var context = HttpContext.CaptureRequestContext();

        // Translate the request into the application command so validation and business logic stay centralized.
        var command = request.ToCommand(context);

        // Delegate authentication to the application layer to attach the user to the active session.
        var user = await mediator.Send(command, ct);

        // Produce a signed JWT that conveys user identity and current session linkage.
        var token = jwt.GenerateToken(userId: user.Id, context.AnonymousSessionId);

        var responseDto = new LoginResponseDto(AccessToken: token,
            TokenType: "Bearer",
            ExpiresInSeconds: jwtOptions.Value.ExpiresInSeconds,
            User: user.ToDto());

        return Ok(responseDto);
    }

    // POST api/auth/anonymous: creates a new anonymous session for guests.
    [HttpPost("anonymous")]
    public async Task<ActionResult<AnonymousSessionDto>> CreateAnonymousSession(CancellationToken ct)
    {
        // Capture minimal caller metadata to seed the anonymous session.
        var requestContext = HttpContext.CaptureRequestContext();

        // Delegate anonymous session creation to the application layer to enforce invariants.
        var command = new CreateAnonymousSessionCommand(requestContext);
        var session = await mediator.Send(command, ct);

        return Ok(session.ToDto());
    }

    // POST api/auth/logout: placeholder to invalidate the caller token/server session.
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Token invalidation is handled by the client until refresh tokens are introduced.
        return Ok();
    }

    // GET api/auth/me: resolves the current authenticated user profile.
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken ct)
    {
        // Resolve user identity using the captured request context.
        var command = new GetCurrentUserCommand(HttpContext.CaptureRequestContext());
        var user = await mediator.Send(command, ct);

        return Ok(user.ToDto());
    }
}
