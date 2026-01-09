using System.Text;
using System.Text.Json;
using Mediator.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Printers.Create;
using Printify.Application.Features.Printers.Delete;
using Printify.Application.Features.Printers.Documents.Clear;
using Printify.Application.Features.Printers.Documents.Get;
using Printify.Application.Features.Printers.Documents.List;
using Printify.Application.Features.Printers.Documents.View;
using Printify.Application.Features.Printers.Get;
using Printify.Application.Features.Printers.List;
using Printify.Application.Features.Printers.Pin;
using Printify.Application.Features.Printers.Sidebar;
using Printify.Application.Features.Printers.Status;
using Printify.Application.Features.Printers.Update;
using Printify.Application.Mediation;
using Printify.Application.Printing;
using Printify.Domain.Config;
using Printify.Domain.Documents;
using Printify.Domain.Documents.View;
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
    private readonly IPrinterDocumentStream documentStream;
    private readonly HttpContextExtensions httpExtensions;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ILogger<PrintersController> logger;
    private readonly ListenerOptions listenerOptions;

    public PrintersController(
        IMediator mediator,
        IPrinterDocumentStream documentStream,
        HttpContextExtensions httpExtensions,
        IOptions<JsonOptions> jsonOptions,
        IOptions<ListenerOptions> listenerOptions,
        ILogger<PrintersController> logger)
    {
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        this.documentStream = documentStream ?? throw new ArgumentNullException(nameof(documentStream));
        this.httpExtensions = httpExtensions;
        ArgumentNullException.ThrowIfNull(jsonOptions);
        this.jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        ArgumentNullException.ThrowIfNull(listenerOptions);
        this.listenerOptions = listenerOptions.Value;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PrinterResponseDto>> Create([FromBody] CreatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var snapshot = await mediator.RequestAsync<CreatePrinterCommand, PrinterDetailsSnapshot>(
            request.ToCommand(httpContext),
            cancellationToken);
        return Ok(snapshot.ToResponseDto(listenerOptions.PublicHost));
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PrinterResponseDto>>> List(CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var printers = await mediator.RequestAsync<ListPrintersQuery, PrinterListResponse>(
            new ListPrintersQuery(httpContext),
            cancellationToken);
        var responses = printers.Printers
            .Select(snapshot => snapshot.ToResponseDto(listenerOptions.PublicHost))
            .ToList();
        return Ok(responses);
    }

    [Authorize]
    [HttpGet("sidebar")]
    public async Task<ActionResult<IReadOnlyList<PrinterSidebarSnapshotDto>>> ListSidebar(CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var snapshots = await mediator.RequestAsync<ListPrinterSidebarQuery, PrinterSidebarListResponse>(
            new ListPrinterSidebarQuery(httpContext),
            cancellationToken);
        var response = snapshots.Snapshots.Select(snapshot => snapshot.ToSidebarSnapshotDto()).ToList();
        return Ok(response);
    }

    [Authorize]
    [HttpGet("sidebar/stream")]
    public async Task StreamSidebar(CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        logger.LogInformation("Sidebar SSE starting for workspace {WorkspaceId}.", httpContext.WorkspaceId);
        var streamResult = await mediator.RequestAsync<StreamPrinterSidebarQuery, PrinterSidebarStreamResult>(
            new StreamPrinterSidebarQuery(httpContext),
            cancellationToken);

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.ContentType = "text/event-stream";
        logger.LogInformation("Sidebar SSE headers set for workspace {WorkspaceId}.", httpContext.WorkspaceId);
        await Response.Body.FlushAsync(cancellationToken);
        logger.LogInformation("Sidebar SSE headers flushed for workspace {WorkspaceId}.", httpContext.WorkspaceId);

        var wroteFirst = false;
        await foreach (var snapshot in streamResult.Updates.WithCancellation(cancellationToken))
        {
            var payload = snapshot.ToSidebarSnapshotDto();
            if (!wroteFirst)
            {
                logger.LogInformation("Sidebar SSE first write starting for workspace {WorkspaceId}.", httpContext.WorkspaceId);
                await WriteSseAsync(streamResult.EventName, payload, cancellationToken);
                logger.LogInformation("Sidebar SSE first write completed for workspace {WorkspaceId}.", httpContext.WorkspaceId);
                wroteFirst = true;
                continue;
            }

            await WriteSseAsync(streamResult.EventName, payload, cancellationToken);
        }
    }

    [Authorize]
    [HttpGet("{id:guid}/runtime/stream")]
    public async Task StreamRuntime(Guid id, CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var streamResult = await mediator.RequestAsync<StreamPrinterRuntimeQuery, PrinterRuntimeStreamResult>(
            new StreamPrinterRuntimeQuery(httpContext, id),
            cancellationToken);

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.ContentType = "text/event-stream";

        await foreach (var statusUpdate in streamResult.Updates.WithCancellation(cancellationToken))
        {
            var payload = statusUpdate.ToStatusUpdateDto(listenerOptions.PublicHost);
            await WriteSseAsync(streamResult.EventName, payload, cancellationToken);
        }
    }

    [Authorize]
    [HttpGet("{id:guid}/documents/stream")]
    public async Task StreamDocuments(Guid id, CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var printer = await mediator.RequestAsync<GetPrinterQuery, PrinterDetailsSnapshot?>(
            new GetPrinterQuery(id, httpContext),
            cancellationToken);
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
        var printer = await mediator.RequestAsync<GetPrinterQuery, PrinterDetailsSnapshot?>(
            new GetPrinterQuery(id, httpContext),
            cancellationToken);
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
            var viewDocument = await mediator.RequestAsync<GetPrinterViewDocumentQuery, ViewDocument?>(
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
        var effectiveLimit = request?.Limit ?? 20;
        var documents = await mediator.RequestAsync<ListPrinterDocumentsQuery, PrinterDocumentListResponse>(
            new ListPrinterDocumentsQuery(
                id,
                httpContext,
                request?.BeforeId,
                effectiveLimit),
            cancellationToken);
        var items = documents.Documents
            .Select(DocumentMapper.ToResponseDto)
            .ToList();
        var hasMore = effectiveLimit > 0 && documents.Documents.Count == effectiveLimit;
        var nextBeforeId = hasMore ? documents.Documents.Last().Id : (Guid?)null;
        var response = new DocumentListResponseDto(
            new Contracts.Common.Pagination.PagedResult<DocumentDto>(
                items,
                hasMore,
                nextBeforeId,
                null));
        return Ok(response);
    }

    [Authorize]
    [HttpGet("{id:guid}/documents/view")]
    public async Task<ActionResult<ViewDocumentListResponseDto>> ListViewDocuments(
        Guid id,
        [FromQuery] GetDocumentsRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var effectiveLimit = request?.Limit ?? 20;
        var documents = await mediator.RequestAsync<ListPrinterViewDocumentsQuery, PrinterViewDocumentListResponse>(
            new ListPrinterViewDocumentsQuery(
                id,
                httpContext,
                request?.BeforeId,
                effectiveLimit),
            cancellationToken);
        var items = documents.Documents
            .Select(ViewDocumentMapper.ToViewResponseDto)
            .ToList();
        var hasMore = effectiveLimit > 0 && documents.Documents.Count == effectiveLimit;
        var nextBeforeId = hasMore ? documents.Documents.Last().Id : (Guid?)null;
        var response = new ViewDocumentListResponseDto(
            new PagedResult<ViewDocumentDto>(
                items,
                hasMore,
                nextBeforeId,
                null));
        return Ok(response);
    }

    // TODO: add tests for ClearPrinterDocuments endpoint.
    [Authorize]
    [HttpDelete("{id:guid}/documents")]
    public async Task<IActionResult> ClearDocuments(Guid id, CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        await mediator.RequestAsync<ClearPrinterDocumentsCommand, Unit>(
            new ClearPrinterDocumentsCommand(httpContext, id),
            cancellationToken);
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
        var document = await mediator.RequestAsync<GetPrinterDocumentQuery, Domain.Documents.Document?>(
            new GetPrinterDocumentQuery(printerId, documentId, httpContext),
            cancellationToken);
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
        var snapshot = await mediator.RequestAsync<GetPrinterQuery, PrinterDetailsSnapshot?>(
            new GetPrinterQuery(id, httpContext),
            cancellationToken);

        if (snapshot is null)
        {
            return NotFound();
        }

        return Ok(snapshot.ToResponseDto(listenerOptions.PublicHost));
    }

    [Authorize]
    [HttpPatch("{id:guid}/operational-flags")]
    public async Task<ActionResult<PrinterOperationalFlagsDto>> UpdateOperationalFlags(
        Guid id,
        [FromBody] UpdatePrinterOperationalFlagsRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var updatedFlags = await mediator.RequestAsync<UpdatePrinterOperationalFlagsCommand, PrinterOperationalFlags>(
            new UpdatePrinterOperationalFlagsCommand(
                httpContext,
                id,
                request.IsCoverOpen,
                request.IsPaperOut,
                request.IsOffline,
                request.HasError,
                request.IsPaperNearEnd,
                request.TargetState),
            cancellationToken);
        return Ok(PrinterMapper.ToOperationalFlagsDto(updatedFlags));
    }

    [Authorize]
    [HttpPatch("{id:guid}/drawers")]
    public async Task<ActionResult<PrinterRuntimeStatusDto>> UpdateDrawerState(
        Guid id,
        [FromBody] UpdatePrinterDrawerStateRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var runtimeStatus = await mediator.RequestAsync<UpdatePrinterDrawerStateCommand, PrinterRuntimeStatus>(
            new UpdatePrinterDrawerStateCommand(
                httpContext,
                id,
                request.Drawer1State,
                request.Drawer2State),
            cancellationToken);
        return Ok(PrinterMapper.ToRuntimeStatusDto(runtimeStatus));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PrinterResponseDto>> Update(Guid id, [FromBody] UpdatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var command = request.ToCommand(id, httpContext);
        var snapshot = await mediator.RequestAsync<UpdatePrinterCommand, PrinterDetailsSnapshot>(command, cancellationToken);
        return Ok(snapshot.ToResponseDto(listenerOptions.PublicHost));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        await mediator.RequestAsync<DeletePrinterCommand, Unit>(new DeletePrinterCommand(httpContext, id), cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{id:guid}/pin")]
    public async Task<ActionResult<PrinterResponseDto>> SetPinned(Guid id, [FromBody] PinPrinterRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = await httpExtensions.CaptureRequestContext(HttpContext);
        var command = request.ToCommand(id, httpContext);
        var snapshot = await mediator.RequestAsync<SetPrinterPinnedCommand, PrinterDetailsSnapshot>(command, cancellationToken);
        return Ok(snapshot.ToResponseDto(listenerOptions.PublicHost));
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
