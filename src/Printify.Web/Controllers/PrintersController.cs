using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Printify.Domain.Printers;
using Printify.Domain.Services;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;
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
    public async Task<ActionResult<PrinterDto>> Create([FromBody] CreatePrinterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
        var ownerUserId = session.ClaimedUserId;
        var metadata = HttpContext.CaptureRequestMetadata(session.Id);

        var saveRequest = DomainMapper.ToSavePrinterRequest(request, ownerUserId, session.Id, metadata.IpAddress);

        var id = await commandService.CreatePrinterAsync(saveRequest, cancellationToken).ConfigureAwait(false);
        var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        if (printer is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return CreatedAtAction(nameof(Get), new { id = printer.Id }, ContractMapper.ToPrinterDto(printer));
    }

    [HttpGet]
    public async Task<ActionResult<PrinterGroupsResponse>> List(CancellationToken cancellationToken)
    {
        var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
        _ = HttpContext.CaptureRequestMetadata(session.Id);

        var temporary = await queryService.ListPrintersAsync(ownerSessionId: session.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
        var temporaryList = ContractMapper.ToPrinterDtos(temporary.Where(printer => printer.OwnerUserId is null));

        IReadOnlyList<PrinterDto> userPrinters = Array.Empty<PrinterDto>();
        if (session.ClaimedUserId is { } userId)
        {
            var owned = await queryService.ListPrintersAsync(ownerUserId: userId, cancellationToken: cancellationToken).ConfigureAwait(false);
            userPrinters = ContractMapper.ToPrinterDtos(owned);
        }

        return Ok(new PrinterGroupsResponse(temporaryList, userPrinters));
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

        var metadata = HttpContext.CaptureRequestMetadata(session.Id);

        foreach (var printerId in request.PrinterIds)
        {
            var printer = await queryService.GetPrinterAsync(printerId, cancellationToken).ConfigureAwait(false);
            if (printer is null || printer.OwnerSessionId != session.Id)
            {
                return NotFound(printerId);
            }

            var updateContract = new UpdatePrinterRequest(
                printer.Id,
                printer.DisplayName,
                printer.Protocol,
                printer.WidthInDots,
                printer.HeightInDots);

            var updateRequest = DomainMapper.ToSavePrinterRequest(
                updateContract,
                session.ClaimedUserId,
                session.Id,
                metadata.IpAddress);

            var updated = await commandService.UpdatePrinterAsync(printer.Id, updateRequest, cancellationToken).ConfigureAwait(false);
            if (!updated)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        return NoContent();
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PrinterDto>> Get(long id, CancellationToken cancellationToken)
    {
        var session = await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
        _ = HttpContext.CaptureRequestMetadata(session.Id);

        var printer = await queryService.GetPrinterAsync(id, cancellationToken).ConfigureAwait(false);
        if (printer is null)
        {
            return NotFound();
        }

        if (printer.OwnerSessionId != session.Id && printer.OwnerUserId != session.ClaimedUserId)
        {
            return NotFound();
        }

        return Ok(ContractMapper.ToPrinterDto(printer));
    }

    public sealed record ResolveTemporaryRequest(IReadOnlyList<long> PrinterIds);

    public sealed record PrinterGroupsResponse(IReadOnlyList<PrinterDto> Temporary, IReadOnlyList<PrinterDto> UserClaimed);
}



