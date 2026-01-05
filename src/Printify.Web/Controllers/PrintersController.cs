using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Printers.Delete;
using Printify.Application.Features.Printers.Documents.Clear;
using Printify.Application.Features.Printers.Documents.Get;
using Printify.Application.Features.Printers.Documents.List;
using Printify.Application.Features.Printers.Documents.View;
using Printify.Application.Features.Printers.Get;
using Printify.Application.Features.Printers.List;
using Printify.Application.Features.Printers.Status;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;
using Printify.Web.Contracts.Common.Pagination;
using Printify.Web.Contracts.Documents.Requests;
using Printify.Web.Contracts.Documents.Responses;
using Printify.Web.Contracts.Documents.Responses.View;
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
    private readonly IPrinterRepository printerRepository;
    private readonly IPrinterDocumentStream documentStream;
    private readonly IPrinterStatusStream statusStream;
    private readonly IPrinterListenerOrchestrator listenerOrchestrator;
    private readonly HttpContextExtensions httpExtensions;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ILogger<PrintersController> logger;

    public PrintersController(
        IMediator mediator,
        IPrinterRepository printerRepository,
        IPrinterDocumentStream documentStream,
        IPrinterStatusStream statusStream,
        IPrinterListenerOrchestrator listenerOrchestrator,
        HttpContextExtensions httpExtensions,
        IOptions<JsonOptions> jsonOptions,
        ILogger<PrintersController> logger)
    {
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        this.printerRepository = printerRepository ?? throw new ArgumentNullException(nameof(printerRepository));
        this.documentStream = documentStream ?? throw new ArgumentNullException(nameof(documentStream));
        this.statusStream = statusStream ?? throw new ArgumentNullException(nameof(statusStream));
        this.listenerOrchestrator = listenerOrchestrator ?? throw new ArgumentNullException(nameof(listenerOrchestrator));
        this.httpExtensions = httpExtensions;
        ArgumentNullException.ThrowIfNull(jsonOptions);
        this.jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PrinterResponseDto>> Create([FromBody] CreatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var printer = await mediator.Send(request.ToCommand(httpContext), cancellationToken);
        var realtimeStatus = await printerRepository.GetRealtimeStatusAsync(printer.Id, cancellationToken);
        return Ok(printer.ToResponseDto(listenerOrchestrator.GetStatus(printer), realtimeStatus));
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PrinterResponseDto>>> List(CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        if (httpContext.WorkspaceId is null)
        {
            return Forbid();
        }

        var printers = await mediator.Send(new ListPrintersQuery(httpContext), cancellationToken);
        var realtimeStatuses = await printerRepository
            .ListRealtimeStatusesAsync(httpContext.WorkspaceId.Value, cancellationToken)
            .ConfigureAwait(false);
        var responses = printers
            .Select(printer =>
            {
                realtimeStatuses.TryGetValue(printer.Id, out var realtimeStatus);
                return printer.ToResponseDto(listenerOrchestrator.GetStatus(printer), realtimeStatus);
            })
            .ToList();
        return Ok(responses);
    }

    [Authorize]
    [HttpGet("status/stream")]
    public async Task StreamStatus(
        [FromQuery] Guid? printerId,
        [FromQuery] string? scope,
        CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        if (httpContext.WorkspaceId is null)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Scope controls payload shape: state-only or full realtime snapshot.
        var normalizedScope = string.IsNullOrWhiteSpace(scope)
            ? "state"
            : scope.Trim().ToLowerInvariant();
        if (normalizedScope is not ("state" or "full"))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (normalizedScope == "full" && printerId is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var statusEvent in statusStream.Subscribe(httpContext.WorkspaceId.Value, cancellationToken))
            {
                if (printerId.HasValue && statusEvent.PrinterId != printerId.Value)
                {
                    continue;
                }

                if (normalizedScope == "state" && HasRealtimePayload(statusEvent))
                {
                    continue;
                }

                if (normalizedScope == "full" && !HasRealtimePayload(statusEvent))
                {
                    continue;
                }

                var payload = PrinterMapper.ToRealtimeStatusDto(statusEvent);
                if (payload is null)
                {
                    continue;
                }

                var eventName = normalizedScope == "state" ? "state" : "full";
                await WriteSseAsync(eventName, payload, cancellationToken);
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
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var printer = await mediator.Send(new GetPrinterQuery(id, httpContext), cancellationToken);
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
    [HttpGet("{id:guid}/documents/view/stream")]
    public async Task StreamViewDocuments(Guid id, CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var printer = await mediator.Send(new GetPrinterQuery(id, httpContext), cancellationToken);
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
                var viewDocument = await mediator.Send(
                    new GetPrinterViewDocumentQuery(
                        id,
                        documentEvent.Document.Id,
                        httpContext),
                    cancellationToken);
                if (viewDocument is not null)
                {
                    await WriteSseAsync(
                        "documentViewReady",
                        ViewDocumentMapper.ToViewResponseDto(viewDocument),
                        cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // client disconnected
        }
    }

    [Authorize]
    [HttpGet("{id:guid}/documents")]
    public async Task<ActionResult<DocumentListResponseDto>> ListDocuments(
        Guid id,
        [FromQuery] GetDocumentsRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        try
        {
            var effectiveLimit = request?.Limit ?? 20;
            var documents = await mediator.Send(
                new ListPrinterDocumentsQuery(
                    id,
                    httpContext,
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
    [HttpGet("{id:guid}/documents/view")]
    public async Task<ActionResult<ViewDocumentListResponseDto>> ListViewDocuments(
        Guid id,
        [FromQuery] GetDocumentsRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        try
        {
            var effectiveLimit = request?.Limit ?? 20;
            var documents = await mediator.Send(
                new ListPrinterViewDocumentsQuery(
                    id,
                    httpContext,
                    request?.BeforeId,
                    effectiveLimit),
                cancellationToken);
            var items = documents
                .Select(ViewDocumentMapper.ToViewResponseDto)
                .ToList();
            var hasMore = effectiveLimit > 0 && documents.Count == effectiveLimit;
            var nextBeforeId = hasMore ? documents.Last().Id : (Guid?)null;
            var response = new ViewDocumentListResponseDto(
                new PagedResult<ViewDocumentDto>(
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

    // TODO: add tests for ClearPrinterDocuments endpoint.
    [Authorize]
    [HttpDelete("{id:guid}/documents")]
    public async Task<IActionResult> ClearDocuments(Guid id, CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        await mediator.Send(new ClearPrinterDocumentsCommand(httpContext, id), cancellationToken);
        return NoContent();
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
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var document = await mediator.Send(new GetPrinterDocumentQuery(printerId, documentId, httpContext), cancellationToken);
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
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var printer = await mediator.Send(new GetPrinterQuery(id, httpContext), cancellationToken);

        if (printer is null)
        {
            return NotFound();
        }

        var realtimeStatus = await printerRepository.GetRealtimeStatusAsync(printer.Id, cancellationToken);
        return Ok(printer.ToResponseDto(listenerOrchestrator.GetStatus(printer), realtimeStatus));
    }

    [Authorize]
    [HttpPatch("{id:guid}/realtime-status")]
    public async Task<ActionResult<PrinterRealtimeStatusDto>> UpdateRealtimeStatus(
        Guid id,
        [FromBody] UpdatePrinterRealtimeStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        try
        {
            var updatedStatus = await mediator.Send(
                new UpdatePrinterRealtimeStatusCommand(
                    httpContext,
                    id,
                    request.TargetStatus,
                    request.IsCoverOpen,
                    request.IsPaperOut,
                    request.IsOffline,
                    request.HasError,
                    request.IsPaperNearEnd,
                    request.Drawer1State,
                    request.Drawer2State),
                cancellationToken);
            return Ok(PrinterMapper.ToRealtimeStatusDto(updatedStatus));
        }
        catch (SocketException ex)
        {
            logger.LogWarning(
                ex,
                "Socket error while starting printer listener for printer {PrinterId} in workspace {WorkspaceId}",
                id,
                httpContext.WorkspaceId);
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"Unable to start listener for this printer: {ex.Message}");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning(
                ex,
                "Invalid realtime status update for printer {PrinterId} in workspace {WorkspaceId}",
                id,
                httpContext.WorkspaceId);
            return ValidationProblem(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PrinterResponseDto>> Update(Guid id, [FromBody] UpdatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        try
        {
            var command = request.ToCommand(id, httpContext);
            var printer = await mediator.Send(command, cancellationToken);
            var realtimeStatus = await printerRepository.GetRealtimeStatusAsync(printer.Id, cancellationToken);
            return Ok(printer.ToResponseDto(listenerOrchestrator.GetStatus(printer), realtimeStatus));
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
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        try
        {
            await mediator.Send(new DeletePrinterCommand(httpContext, id), cancellationToken);
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

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        try
        {
            var command = request.ToCommand(id, httpContext);
            var printer = await mediator.Send(command, cancellationToken);
            var realtimeStatus = await printerRepository.GetRealtimeStatusAsync(printer.Id, cancellationToken);
            return Ok(printer.ToResponseDto(listenerOrchestrator.GetStatus(printer), realtimeStatus));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
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

    private static bool HasRealtimePayload(PrinterRealtimeStatus status)
    {
        return status.BufferedBytes.HasValue
               || status.IsCoverOpen.HasValue
               || status.IsPaperOut.HasValue
               || status.IsOffline.HasValue
               || status.HasError.HasValue
               || status.IsPaperNearEnd.HasValue
               || status.Drawer1State.HasValue
               || status.Drawer2State.HasValue;
    }

}
