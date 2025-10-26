
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printify.Application.Features.Printers.Delete;
using Printify.Application.Features.Printers.Get;
using Printify.Application.Features.Printers.List;
using Printify.Application.Features.Printers.Update;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PrintersController : ControllerBase
{
    private readonly IMediator mediator;

    public PrintersController(IMediator mediator)
    {
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PrinterDto>> Create([FromBody] CreatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await mediator.Send(request.ToCommand(HttpContext.CaptureRequestContext()), cancellationToken);
        return Ok(PrinterMapper.ToDto(printer));
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PrinterDto>>> List(CancellationToken cancellationToken)
    {
        var context = HttpContext.CaptureRequestContext();
        if (context.UserId is null && context.AnonymousSessionId is null)
        {
            return Forbid();
        }

        var printers = await mediator.Send(new ListPrintersQuery(context), cancellationToken);
        return Ok(printers.ToDtos());
    }

    [HttpPost("resolveTemporary")]
    public Task<IActionResult> ResolveTemporary([FromBody] ResolveTemporaryRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IActionResult>(NoContent());
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrinterDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var context = HttpContext.CaptureRequestContext();
        var printer = await mediator.Send(new GetPrinterQuery(id, context), cancellationToken);

        if (printer is null)
        {
            return NotFound();
        }

        return Ok(PrinterMapper.ToDto(printer));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PrinterDto>> Update(Guid id, [FromBody] UpdatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = HttpContext.CaptureRequestContext();
        try
        {
            var command = request.ToCommand(id, context);
            var printer = await mediator.Send(command, cancellationToken);
            return Ok(PrinterMapper.ToDto(printer));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var context = HttpContext.CaptureRequestContext();
        try
        {
            await mediator.Send(new DeletePrinterCommand(context, id), cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [Authorize]
    [HttpPost("{id:guid}/pin")]
    public async Task<ActionResult<PrinterDto>> SetPinned(Guid id, [FromBody] PinPrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = HttpContext.CaptureRequestContext();
        try
        {
            var command = request.ToCommand(id, context);
            var printer = await mediator.Send(command, cancellationToken);
            return Ok(PrinterMapper.ToDto(printer));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    public sealed record ResolveTemporaryRequest(IReadOnlyList<long> PrinterIds);
}
