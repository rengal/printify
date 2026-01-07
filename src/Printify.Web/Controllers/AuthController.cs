using Mediator.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Auth.GetCurrentWorkspace;
using Printify.Application.Features.Auth.Login;
using Printify.Application.Interfaces;
using Printify.Domain.Config;
using Printify.Domain.Workspaces;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Workspaces.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IOptions<JwtOptions> jwtOptions, IMediator mediator, IJwtTokenGenerator jwt, HttpContextExtensions httpExtensions) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody]LoginRequestDto request,
        CancellationToken ct)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var command = request.ToCommand(httpContext);
        var workspace = await mediator.RequestAsync<LoginCommand, Workspace>(command, ct);

        var token = jwt.GenerateToken(workspace.Id);

        var responseDto = new LoginResponseDto(AccessToken: token,
            TokenType: "Bearer",
            ExpiresInSeconds: jwtOptions.Value.ExpiresInSeconds,
            Workspace: workspace.ToDto());

        return Ok(responseDto);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Token invalidation is handled by the client until refresh tokens are introduced.
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<WorkspaceDto>> GetCurrentWorkspace(CancellationToken ct)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var command = new GetCurrentWorkspaceCommand(httpContext);
        var workspace = await mediator.RequestAsync<GetCurrentWorkspaceCommand, Workspace>(command, ct);

        return Ok(workspace.ToDto());
    }
}
