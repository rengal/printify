using Mediator.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printify.Application.Features.Workspaces.CreateWorkspace;
using Printify.Application.Features.Workspaces.GetGreeting;
using Printify.Application.Features.Workspaces.GetWorkspaceSummary;
using Printify.Application.Services;
using Printify.Domain.Workspaces;
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

        var workspace = await mediator.RequestAsync<CreateWorkspaceCommand, Workspace>(command, ct).ConfigureAwait(false);
        var workspaceDto = workspace.ToResponseDto();

        return Ok(workspaceDto);
    }

    [Authorize]
    [HttpGet("summary")]
    public async Task<ActionResult<WorkspaceSummaryDto>> GetSummary(CancellationToken ct)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var query = new GetWorkspaceSummaryQuery(httpContext);

        var summary = await mediator.RequestAsync<GetWorkspaceSummaryQuery, WorkspaceSummary>(query, ct).ConfigureAwait(false);
        var summaryDto = summary.ToDto();

        return Ok(summaryDto);
    }

    [AllowAnonymous]
    [HttpGet("greeting")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, NoStore = false)]
    public async Task<ActionResult<GreetingResponseDto>> GetGreeting(CancellationToken ct)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var query = new GetGreetingQuery(httpContext);

        var greeting = await mediator.RequestAsync<GetGreetingQuery, GreetingResponse>(query, ct).ConfigureAwait(false);
        var greetingDto = greeting.ToDto();

        return Ok(greetingDto);
    }
}
