using MediatR;
using Microsoft.AspNetCore.Mvc;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WorkspacesController(IMediator mediator, HttpContextExtensions httpExtensions) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> CreateWorkspace(
        [FromBody] CreateWorkspaceRequestDto request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var command = request.ToCommand(httpContext);

        var workspace = await mediator.Send(command, ct).ConfigureAwait(false);
        var workspaceDto = workspace.ToResponseDto();

        return Ok(workspaceDto);
    }
}
