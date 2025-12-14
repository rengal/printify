using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    private readonly IPrinterListenerOrchestrator listenerOrchestrator;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ILogger<PrintersController> logger;

    public PrintersController(
        IMediator mediator,
        IPrinterDocumentStream documentStream,
        IPrinterStatusStream statusStream,
        IPrinterListenerOrchestrator listenerOrchestrator,
        IOptions<JsonOptions> jsonOptions,
        ILogger<PrintersController> logger)
    {
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        this.documentStream = documentStream ?? throw new ArgumentNullException(nameof(documentStream));
        this.statusStream = statusStream ?? throw new ArgumentNullException(nameof(statusStream));
        this.listenerOrchestrator = listenerOrchestrator ?? throw new ArgumentNullException(nameof(listenerOrchestrator));
        ArgumentNullException.ThrowIfNull(jsonOptions);
        this.jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        var responses = printers
            .Select(printer => printer.ToResponseDto(listenerOrchestrator.GetStatus(printer)))
            .ToList();
        return Ok(responses);
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
                await WriteSseAsync("status", statusEvent.ToResponseDto(), cancellationToken);
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
                await WriteSseAsync("documentReady", DocumentMapper.ToResponseDto(documentEvent.Document), cancellationToken);
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
        [FromQuery] GetDocumentsRequestDto? request,
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

        return Ok(printer.ToResponseDto(listenerOrchestrator.GetStatus(printer)));
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
            return Ok(printer.ToResponseDto(listenerOrchestrator.GetStatus(printer)));
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
            logger.LogInformation(
                "Received request to set printer {PrinterId} target state to {TargetStatus} for workspace {WorkspaceId}",
                id,
                request.TargetStatus,
                context.WorkspaceId);

            var command = new SetPrinterTargetStateCommand(
                context,
                id,
                request.TargetStatus.ToTargetState());
            var printer = await mediator.Send(command, cancellationToken);
            return Ok(printer.ToResponseDto(listenerOrchestrator.GetStatus(printer)));
        }
        catch (SocketException ex)
        {
            logger.LogWarning(
                ex,
                "Socket error while starting printer listener for printer {PrinterId} in workspace {WorkspaceId}",
                id,
                context.WorkspaceId);
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"Unable to start listener for this printer: {ex.Message}");
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning(
                "Printer {PrinterId} not found when attempting to set status for workspace {WorkspaceId}",
                id,
                context.WorkspaceId);
            return NotFound();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning(
                ex,
                "Invalid target status {TargetStatus} for printer {PrinterId} in workspace {WorkspaceId}",
                request.TargetStatus,
                id,
                context.WorkspaceId);
            return ValidationProblem(ex.Message);
        }
    }

    private async Task WriteSseAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Serialize(payload, jsonOptions);
        var builder = new StringBuilder();
        builder.Append("event: ").Append(eventName).Append('\n');
        builder.Append("data: ").Append(data).Append("\n\n");
        await Response.WriteAsync(builder.ToString(), cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
