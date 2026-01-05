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
using Printify.Domain.Mapping;
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
    private readonly IPrinterListenerOrchestrator listenerOrchestrator;
    private readonly HttpContextExtensions httpExtensions;
    private readonly JsonSerializerOptions jsonOptions;

    public PrintersController(
        IMediator mediator,
        IPrinterRepository printerRepository,
        IPrinterDocumentStream documentStream,
        IPrinterListenerOrchestrator listenerOrchestrator,
        HttpContextExtensions httpExtensions,
        IOptions<JsonOptions> jsonOptions)
    {
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        this.printerRepository = printerRepository ?? throw new ArgumentNullException(nameof(printerRepository));
        this.documentStream = documentStream ?? throw new ArgumentNullException(nameof(documentStream));
        this.listenerOrchestrator = listenerOrchestrator ?? throw new ArgumentNullException(nameof(listenerOrchestrator));
        this.httpExtensions = httpExtensions;
        ArgumentNullException.ThrowIfNull(jsonOptions);
        this.jsonOptions = jsonOptions.Value.JsonSerializerOptions;
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
        var streamResult = await mediator.Send(
            new StreamPrinterStatusQuery(
                httpContext,
                printerId,
                DomainMapper.ParsePrinterRealtimeScope(scope)),
            cancellationToken);

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.ContentType = "text/event-stream";

        await foreach (var statusUpdate in streamResult.Updates.WithCancellation(cancellationToken))
        {
            var payload = PrinterMapper.ToRealtimeStatusUpdateDto(statusUpdate);
            if (payload is null)
            {
                continue;
            }

            await WriteSseAsync(streamResult.EventName, payload, cancellationToken);
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

        await foreach (var documentEvent in documentStream.Subscribe(id, cancellationToken))
        {
            await WriteSseAsync("documentReady", DocumentMapper.ToResponseDto(documentEvent.Document), cancellationToken);
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

}
