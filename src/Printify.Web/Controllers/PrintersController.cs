using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.Contracts.Sessions;
using Printify.Web.Security;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PrintersController : ControllerBase
{
    private readonly IResourceCommandService commandService;
    private readonly IResourceQueryService queryService;
    private readonly ISessionService sessionService;

    public PrintersController(IResourceCommandService commandService, IResourceQueryService queryService, ISessionService sessionService)
    {
        this.commandService = commandService;
        this.queryService = queryService;
        this.sessionService = sessionService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrinterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
        var ownerUserId = session.ClaimedUserId;
        var now = DateTimeOffset.UtcNow;
        session = session with { LastActiveAt = now, ExpiresAt = now.Add(SessionManager.SessionLifetime) };
        await sessionService.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
        var createdFromIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var saveRequest = new SavePrinterRequest(
            ownerUserId,
            session.Id,
            request.DisplayName,
            request.Protocol,
            request.WidthInDots,
            request.HeightInDots,
            createdFromIp);

        var id = await commandService.CreatePrinterAsync(saveRequest, cancellationToken).ConfigureAwait(false);
        var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        if (printer is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return CreatedAtAction(nameof(Get), new { id = printer.Id }, printer);
    }

    [HttpGet]
    public async Task<ActionResult<PrinterListResponse>> List(CancellationToken cancellationToken)
    {
        var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);

        var nowList = DateTimeOffset.UtcNow;
        session = session with { LastActiveAt = nowList, ExpiresAt = nowList.Add(SessionManager.SessionLifetime) };
        await sessionService.UpdateAsync(session, cancellationToken).ConfigureAwait(false);

        var temporary = await queryService.ListPrintersAsync(ownerSessionId: session.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
        var temporaryList = temporary.Where(printer => printer.OwnerUserId is null).ToList();

        IReadOnlyList<Printer> userPrinters = Array.Empty<Printer>();
        if (session.ClaimedUserId is { } userId)
        {
            userPrinters = await queryService.ListPrintersAsync(ownerUserId: userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return Ok(new PrinterListResponse(temporaryList, userPrinters));
    }

    [HttpPost("resolveTemporary")]
    public async Task<IActionResult> ResolveTemporary([FromBody] ResolveTemporaryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.PrinterIds.Count == 0)
        {
            return BadRequest("At least one printer id must be provided.");
        }

        var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
        if (session.ClaimedUserId is null)
        {
            return BadRequest("Login required to claim printers.");
        }

        foreach (var printerId in request.PrinterIds)
        {
            var printer = await queryService.GetPrinterAsync(printerId, cancellationToken).ConfigureAwait(false);
            if (printer is null || printer.OwnerSessionId != session.Id)
            {
                return NotFound(printerId);
            }

            var updateRequest = new SavePrinterRequest(
                session.ClaimedUserId,
                session.Id,
                printer.DisplayName,
                printer.Protocol,
                printer.WidthInDots,
                printer.HeightInDots,
                printer.CreatedFromIp);

            var updated = await commandService.UpdatePrinterAsync(printer.Id, updateRequest, cancellationToken).ConfigureAwait(false);
            if (!updated)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        var refresh = DateTimeOffset.UtcNow;
        session = session with { LastActiveAt = refresh, ExpiresAt = refresh.Add(SessionManager.SessionLifetime) };
        await sessionService.UpdateAsync(session, cancellationToken).ConfigureAwait(false);

        return NoContent();
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Printer>> Get(long id, CancellationToken cancellationToken)
    {
        var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
        var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        if (printer is null)
        {
            return NotFound();
        }

        if (printer.OwnerSessionId != session.Id && printer.OwnerUserId != session.ClaimedUserId)
        {
            return NotFound();
        }

        return Ok(printer);
    }

    public sealed record CreatePrinterRequest(string DisplayName, string Protocol, int WidthInDots, int? HeightInDots);
    public sealed record ResolveTemporaryRequest(IReadOnlyList<long> PrinterIds);
    public sealed record PrinterListResponse(IReadOnlyList<Printer> Temporary, IReadOnlyList<Printer> UserClaimed);
}



