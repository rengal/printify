using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Auth.GetCurrentWorkspace;
using Printify.Application.Interfaces;
using Printify.Infrastructure.Config;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Workspaces.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IOptions<JwtOptions> jwtOptions, IMediator mediator, IJwtTokenGenerator jwt) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody]LoginRequestDto request,
        CancellationToken ct)
    {
        var command = request.ToCommand(HttpContext.CaptureRequestContext());
        var workspace= await mediator.Send(command, ct);

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
        var command = new GetCurrentWorkspaceCommand(HttpContext.CaptureRequestContext());
        var workspace = await mediator.Send(command, ct);

        return Ok(workspace.ToDto());
    }
}
