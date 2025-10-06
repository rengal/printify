using System;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Services;
using Printify.Contracts.Users;
using Printify.Web.Security;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IResourceCommandService commandService;
    private readonly IResourceQueryService queryService;

    public AuthController(IResourceCommandService commandService, IResourceQueryService queryService)
    {
        this.commandService = commandService;
        this.queryService = queryService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Username);

        var username = request.Username.Trim();
        var user = await queryService.FindUserByNameAsync(username, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await commandService.CreateUserAsync(new SaveUserRequest(username, clientIp), cancellationToken).ConfigureAwait(false);
            user = await queryService.FindUserByNameAsync(username, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to persist user record.");
            }
        }

        var token = TokenService.IssueToken(username, DateTimeOffset.UtcNow, out _);
        var response = new AuthResponse(token, TokenService.DefaultExpirySeconds, new UserResponse(user.Id, user.DisplayName));
        return Ok(response);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Token management is client-driven; acknowledge the request.
        return Ok(new { Success = true });
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!TokenService.TryExtractUsername(HttpContext, out var username))
        {
            return Unauthorized();
        }

        var user = await queryService.FindUserByNameAsync(username, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new UserResponse(user.Id, user.DisplayName));
    }

    public sealed record LoginRequest(string Username);
    public sealed record AuthResponse(string Token, int ExpiresIn, UserResponse User);
    public sealed record UserResponse(long Id, string Name);
}