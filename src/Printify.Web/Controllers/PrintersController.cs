using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Printers.Delete;
using Printify.Application.Features.Printers.Documents.Get;
using Printify.Application.Features.Printers.Documents.List;
using Printify.Application.Features.Printers.Get;
using Printify.Application.Features.Printers.List;
using Printify.Application.Features.Printers.Status;
using Printify.Application.Printing;
using Printify.Web.Contracts.Documents.Requests;
using Printify.Web.Contracts.Documents.Responses;
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
    private readonly IPrinterStatusStream statusStream;
    private readonly JsonSerializerOptions jsonOptions;

    public PrintersController(
        IMediator mediator,
        IPrinterDocumentStream documentStream,
        IPrinterStatusStream statusStream,
        IOptions<JsonOptions> jsonOptions)
    {
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        this.documentStream = documentStream ?? throw new ArgumentNullException(nameof(documentStream));
        this.statusStream = statusStream ?? throw new ArgumentNullException(nameof(statusStream));
        ArgumentNullException.ThrowIfNull(jsonOptions);
        this.jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PrinterResponseDto>> Create([FromBody] CreatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await mediator.Send(request.ToCommand(HttpContext.CaptureRequestContext()), cancellationToken);
        return Ok(printer.ToResponseDto());
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PrinterResponseDto>>> List(CancellationToken cancellationToken)
    {
        var context = HttpContext.CaptureRequestContext();
        if (context.WorkspaceId is null)
        {
            return Forbid();
        }

        var printers = await mediator.Send(new ListPrintersQuery(context), cancellationToken);
        return Ok(printers.Select(printer => printer.ToResponseDto()).ToList());
    }

    [Authorize]
    [HttpGet("status/stream")]
    public async Task StreamStatus(CancellationToken cancellationToken)
    {
        var context = HttpContext.CaptureRequestContext();
        if (context.WorkspaceId is null)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var statusEvent in statusStream.Subscribe(context.WorkspaceId.Value, cancellationToken))
            {
                var payload = JsonSerializer.Serialize(statusEvent, jsonOptions);
                await Response.WriteAsync("event: status\n", cancellationToken);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // client disconnected
        }
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
    public async Task<ActionResult<DocumentListResponseDto>> ListDocuments(
        Guid id,
        [FromQuery] GetDocumentsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var context = HttpContext.CaptureRequestContext();
        try
        {
            var effectiveLimit = request?.Limit ?? 20;
            var documents = await mediator.Send(
                new ListPrinterDocumentsQuery(
                    id,
                    context,
                    request?.BeforeId,
                    effectiveLimit),
                cancellationToken);
            var items = documents
                .Select(DocumentMapper.ToResponseDto)
                .ToList();
            var hasMore = effectiveLimit > 0 && documents.Count == effectiveLimit;
            var nextBeforeId = hasMore ? documents.Last().Id : (Guid?)null;
            var response = new DocumentListResponseDto(
                new Contracts.Common.Pagination.PagedResult<DocumentDto>(
                    items,
                    hasMore,
                    nextBeforeId,
                    null));
            return Ok(response);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [Authorize]
    [HttpPost("{id:guid}/documents/last-viewed")]
    public IActionResult SetLastViewedDocument(Guid id, [FromBody] SetLastViewedDocumentRequestDto request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
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

        return Ok(DocumentMapper.ToResponseDto(document));
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrinterResponseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var context = HttpContext.CaptureRequestContext();
        var printer = await mediator.Send(new GetPrinterQuery(id, context), cancellationToken);

        if (printer is null)
        {
            return NotFound();
        }

        return Ok(printer.ToResponseDto());
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PrinterResponseDto>> Update(Guid id, [FromBody] UpdatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = HttpContext.CaptureRequestContext();
        try
        {
            var command = request.ToCommand(id, context);
            var printer = await mediator.Send(command, cancellationToken);
            return Ok(printer.ToResponseDto());
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
    public async Task<ActionResult<PrinterResponseDto>> SetPinned(Guid id, [FromBody] PinPrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = HttpContext.CaptureRequestContext();
        try
        {
            var command = request.ToCommand(id, context);
            var printer = await mediator.Send(command, cancellationToken);
            return Ok(printer.ToResponseDto());
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [Authorize]
    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<PrinterResponseDto>> SetStatus(Guid id, [FromBody] SetPrinterStatusRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = HttpContext.CaptureRequestContext();
        try
        {
            var command = new SetPrinterDesiredStatusCommand(
                context,
                id,
                request.DesiredStatus.ToDesiredStatus());
            var printer = await mediator.Send(command, cancellationToken);
            return Ok(printer.ToResponseDto());
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }
}
