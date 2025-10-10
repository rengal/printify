using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Queries;
using Printify.Domain.Printers;
using Printify.Domain.Services;
using Printify.Domain.Sessions;
using Printify.Web.Contracts.Common.Pagination;
using Printify.Web.Contracts.Documents.Requests;
using Printify.Web.Contracts.Documents.Responses;
using Printify.Web.Contracts.Documents.Responses.Elements;
using Printify.Web.Infrastructure;
using Printify.Web.Mapping;
using Printify.Web.Security;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private const int DefaultLimit = 20;
    private readonly IResourceCommandService commandService;
    private readonly IResourceQueryService queryService;
    private readonly ISessionService sessionService;

    public DocumentsController(IResourceCommandService commandService, IResourceQueryService queryService, ISessionService sessionService)
    {
        this.commandService = commandService;
        this.queryService = queryService;
        this.sessionService = sessionService;
    }

    [HttpPost]
    public async Task<ActionResult<DocumentDto>> CreateDocument(
        [FromBody] CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await GetSessionContextAsync(cancellationToken).ConfigureAwait(false);
        var metadata = HttpContext.CaptureRequestMetadata(session.Id);

        var printer = await queryService.GetPrinterAsync(request.PrinterId, cancellationToken).ConfigureAwait(false);
        if (printer is null || !IsPrinterAccessible(printer, session))
        {
            return NotFound();
        }

        var saveRequest = DomainMapper.ToSaveDocumentRequest(request, metadata.IpAddress);
        var documentId = await commandService.CreateDocumentAsync(saveRequest, cancellationToken).ConfigureAwait(false);
        var document = await queryService.GetDocumentAsync(documentId, includeContent: false, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        var dto = ContractMapper.ToDocumentDto(document);
        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, dto);
    }

    [HttpGet]
    public async Task<ActionResult<DocumentListResponse>> GetDocuments(
        [FromQuery] long printerId,
        [FromQuery] int? limit,
        [FromQuery] long? beforeId,
        [FromQuery] string? sourceIp,
        CancellationToken cancellationToken)
    {
        var session = await GetSessionContextAsync(cancellationToken).ConfigureAwait(false);
        var metadata = HttpContext.CaptureRequestMetadata(session.Id);

        var printer = await queryService.GetPrinterAsync(printerId, cancellationToken).ConfigureAwait(false);
        if (printer is null || !IsPrinterAccessible(printer, session))
        {
            return NotFound();
        }

        var resolvedLimit = limit.GetValueOrDefault(DefaultLimit);
        var query = new ListQuery(resolvedLimit, beforeId, sourceIp ?? metadata.IpAddress);
        var result = await queryService.ListDocumentsAsync(query, cancellationToken).ConfigureAwait(false);

        var filteredItems = result.Items
            .Where(descriptor => descriptor.PrinterId == printerId)
            .Select(ContractMapper.ToDocumentDto)
            .ToList();

        var pagedResult = new PagedResult<DocumentDto>(
            filteredItems,
            result.HasMore || filteredItems.Count < result.Items.Count,
            result.NextBeforeId);

        return Ok(new DocumentListResponse(pagedResult));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<DocumentDto>> GetDocument(
        long id,
        [FromQuery] bool includeContent,
        CancellationToken cancellationToken)
    {
        var session = await GetSessionContextAsync(cancellationToken).ConfigureAwait(false);
        _ = HttpContext.CaptureRequestMetadata(session.Id);

        var document = await queryService.GetDocumentAsync(id, includeContent, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return NotFound();
        }

        var printer = await queryService.GetPrinterAsync(document.PrinterId, cancellationToken).ConfigureAwait(false);
        if (printer is null || !IsPrinterAccessible(printer, session))
        {
            return NotFound();
        }

        var dto = ContractMapper.ToDocumentDto(document);
        return Ok(dto);
    }

    private async ValueTask<Session> GetSessionContextAsync(CancellationToken cancellationToken)
    {
        return await SessionManager.GetOrCreateSessionAsync(HttpContext, sessionService, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsPrinterAccessible(Printer printer, Session session)
    {
        if (printer.OwnerSessionId == session.Id)
        {
            return true;
        }

        if (printer.OwnerUserId is not null && session.ClaimedUserId == printer.OwnerUserId)
        {
            return true;
        }

        return false;
    }
}
