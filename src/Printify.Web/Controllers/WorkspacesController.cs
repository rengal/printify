using Mediator.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printify.Application.Features.Auth.GetCurrentWorkspace;
using Printify.Application.Features.Workspaces.CreateWorkspace;
using Printify.Application.Features.Workspaces.DeleteWorkspace;
using Printify.Application.Features.Workspaces.GetGreeting;
using Printify.Application.Features.Workspaces.GetAdminWorkspaceStatistics;
using Printify.Application.Features.Workspaces.GetWorkspaceSummary;
using Printify.Application.Features.Workspaces.UpdateWorkspace;
using Printify.Application.Printing;
using Printify.Application.Services;
using Printify.Domain.Workspaces;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WorkspacesController(
    IMediator mediator,
    HttpContextExtensions httpExtensions,
    ITcpConnectionLog connectionLog)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> CreateWorkspace(
        [FromBody] CreateWorkspaceRequestDto request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var command = request.ToCommand(httpContext);

        var workspace = await mediator.RequestAsync<CreateWorkspaceCommand, Workspace>(command, ct)
            .ConfigureAwait(false);
        var workspaceDto = workspace.ToResponseDto();

        return Ok(workspaceDto);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<WorkspaceDto>> GetWorkspace(CancellationToken ct)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var command = new GetCurrentWorkspaceCommand(httpContext);
        var workspace = await mediator.RequestAsync<GetCurrentWorkspaceCommand, Workspace>(command, ct)
            .ConfigureAwait(false);

        // Admin workspaces can inspect foreign printers; do not let admin browser traffic pollute IP whitelist hints.
        if (workspace.Role != WorkspaceRole.Admin
            && httpContext.WorkspaceId.HasValue
            && !string.IsNullOrEmpty(httpContext.IpAddress))
        {
            connectionLog.Record(
                httpContext.WorkspaceId.Value,
                httpContext.IpAddress,
                allowed: true,
                ConnectionType.Web);
        }

        return Ok(workspace.ToDto());
    }

    [Authorize]
    [HttpGet("summary")]
    public async Task<ActionResult<WorkspaceSummaryDto>> GetSummary(CancellationToken ct)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var query = new GetWorkspaceSummaryQuery(httpContext);

        var summary = await mediator.RequestAsync<GetWorkspaceSummaryQuery, WorkspaceSummary>(query, ct)
            .ConfigureAwait(false);
        var summaryDto = summary.ToDto();

        return Ok(summaryDto);
    }

    [Authorize]
    [HttpGet("admin-statistics")]
    public async Task<ActionResult<AdminWorkspaceStatisticsDto>> GetAdminStatistics(CancellationToken ct)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var query = new GetAdminWorkspaceStatisticsQuery(httpContext);

        var statistics = await mediator
            .RequestAsync<GetAdminWorkspaceStatisticsQuery, AdminWorkspaceStatistics>(query, ct)
            .ConfigureAwait(false);

        return Ok(statistics.ToDto());
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

    [Authorize]
    [HttpPatch]
    public async Task<ActionResult<WorkspaceDto>> UpdateWorkspace(
        [FromBody] UpdateWorkspaceRequestDto request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);

        // Validate DocumentRetentionDays range if provided
        if (request.DocumentRetentionDays.HasValue)
        {
            if (request.DocumentRetentionDays.Value < 0 || request.DocumentRetentionDays.Value > 365)
            {
                return BadRequest(new { error = "DocumentRetentionDays must be between 0 and 365" });
            }
        }

        var command = new UpdateWorkspaceCommand(
            httpContext,
            request.Name,
            request.DocumentRetentionDays,
            request.TcpWhitelistEnabled,
            request.TcpWhitelistEntries);
        var workspace = await mediator.RequestAsync<UpdateWorkspaceCommand, Workspace>(command, ct)
            .ConfigureAwait(false);

        return Ok(workspace.ToDto());
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> DeleteWorkspace(CancellationToken ct)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var command = new DeleteWorkspaceCommand(httpContext);

        await mediator.RequestAsync<DeleteWorkspaceCommand, DeleteWorkspaceResult>(command, ct).ConfigureAwait(false);

        return NoContent();
    }

    [Authorize]
    [HttpGet("connections")]
    public async Task<ActionResult<IReadOnlyList<TcpConnectionEntryDto>>> GetRecentConnections(
        [FromQuery] int? minutes,
        CancellationToken ct)
    {
        var ctx = httpExtensions.GetRequestContext(HttpContext);
        if (ctx.WorkspaceId is null)
            return Unauthorized();

        var workspace = await mediator.RequestAsync<GetCurrentWorkspaceCommand, Workspace>(
                new GetCurrentWorkspaceCommand(ctx),
                ct)
            .ConfigureAwait(false);

        var window = minutes is > 0 and <= 1440
            ? TimeSpan.FromMinutes(minutes.Value)
            : (TimeSpan?)null;

        var entries = connectionLog.GetRecent(ctx.WorkspaceId.Value, window);

        // For Web connections keep only the latest entry per IP (deduplicate repeated polls).
        // For TCP connections keep all entries (each attempt is meaningful).
        // Admin browser traffic is not a useful whitelist hint because admins can inspect foreign workspaces.
        var dtos = entries
            .Where(e => workspace.Role != WorkspaceRole.Admin || e.ConnectionType != ConnectionType.Web)
            .OrderByDescending(e => e.ConnectedAt)
            .GroupBy(e => (e.ConnectionType, e.ClientIp))
            .SelectMany(g => g.Key.ConnectionType == ConnectionType.Web ? g.Take(1) : g)
            .OrderByDescending(e => e.ConnectedAt)
            .Select(e => new TcpConnectionEntryDto(e.ClientIp, e.ConnectedAt, e.Allowed, e.ConnectionType.ToString()))
            .ToList();

        return Ok(dtos);
    }
}
