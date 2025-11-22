
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Printers.Delete;
using Printify.Application.Features.Printers.Documents.Get;
using Printify.Application.Features.Printers.Documents.List;
using Printify.Application.Features.Printers.Get;
using Printify.Application.Features.Printers.List;
using Printify.Application.Features.Printers.Update;
using Printify.Application.Printing;
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
    private readonly IPrinterDocumentStream documentStream;
    private readonly JsonSerializerOptions jsonOptions;

    public PrintersController(
        IMediator mediator,
        IPrinterDocumentStream documentStream,
        IOptions<JsonOptions> jsonOptions)
    {
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        this.documentStream = documentStream ?? throw new ArgumentNullException(nameof(documentStream));
        ArgumentNullException.ThrowIfNull(jsonOptions);
        this.jsonOptions = jsonOptions.Value.JsonSerializerOptions;
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
    [HttpGet("{id:guid}/documents/stream")]
    public async Task StreamDocuments(Guid id, CancellationToken cancellationToken)
    {
        var context = HttpContext.CaptureRequestContext();
        var printer = await mediator.Send(new GetPrinterQuery(id, context), cancellationToken);
        if (printer is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var documentEvent in documentStream.Subscribe(id, cancellationToken))
            {
                var payload = JsonSerializer.Serialize(documentEvent.Document, jsonOptions);
                await Response.WriteAsync("event: documentReady\n", cancellationToken);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Canceled");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [Authorize]
    [HttpGet("{id:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<Domain.Documents.Document>>> ListDocuments(
        Guid id,
        [FromQuery] DateTimeOffset? beforeCreatedAt = null,
        [FromQuery] Guid? beforeId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var context = HttpContext.CaptureRequestContext();
        try
        {
            var documents = await mediator.Send(
                new ListPrinterDocumentsQuery(id, context, beforeCreatedAt, beforeId, from, to, limit),
                cancellationToken);
            return Ok(documents);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [Authorize]
    [HttpGet("{printerId:guid}/documents/{documentId:guid}")]
    public async Task<ActionResult<Domain.Documents.Document>> GetDocument(Guid printerId, Guid documentId, CancellationToken cancellationToken)
    {
        var context = HttpContext.CaptureRequestContext();
        var document = await mediator.Send(new GetPrinterDocumentQuery(printerId, documentId, context), cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        return Ok(document);
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
