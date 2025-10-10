using System.Net;
using Microsoft.AspNetCore.Mvc;
using Printify.Domain.Services;
using Printify.Domain.Users;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Users.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;
using Printify.Web.Security;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IResourceCommandService commandService;
    private readonly IResourceQueryService queryService;
    private readonly ISessionService sessionService;

    public AuthController(IResourceCommandService commandService, IResourceQueryService queryService, ISessionService sessionService)
    {
        this.commandService = commandService;
        this.queryService = queryService;
        this.sessionService = sessionService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Username);

        var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
        var metadata = HttpContext.CaptureRequestMetadata(session.Id);
        var username = request.Username.Trim();

        var user = await queryService.FindUserByNameAsync(username, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            var id = await commandService.CreateUserAsync(new SaveUserRequest(username, metadata.IpAddress), cancellationToken).ConfigureAwait(false);
            user = await queryService.GetUserAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to persist or retrieve user record.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        session = session with { ClaimedUserId = user.Id, LastActiveAt = now, ExpiresAt = now.Add(SessionManager.SessionLifetime) };
        await sessionService.UpdateAsync(session, cancellationToken).ConfigureAwait(false);

        return Ok(ContractMapper.ToUserDto(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        _ = HttpContext.CaptureRequestMetadata(null);

        if (HttpContext.Request.Cookies.TryGetValue(SessionManager.SessionCookieName, out var cookie) &&
            long.TryParse(cookie, out var sessionId))
        {
            await sessionService.DeleteAsync(sessionId, cancellationToken).ConfigureAwait(false);
        }

        SessionManager.ClearSessionCookie(HttpContext);
        return Ok(new { Success = true });
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
        _ = HttpContext.CaptureRequestMetadata(session.Id);

        if (session.ClaimedUserId is null)
        {
            return Unauthorized();
        }

        var user = await queryService.GetUserAsync(session.ClaimedUserId.Value, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(ContractMapper.ToUserDto(user));
    }
}
