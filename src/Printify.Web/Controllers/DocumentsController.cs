using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Queries;
using Printify.Domain.Printers;
using Printify.Domain.Services;
using Printify.Domain.Sessions;
using Printify.Web.Security;
using DocumentDescriptor = Printify.Contracts.Documents.DocumentDescriptor;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private const int DefaultLimit = 20;
    private readonly IResourceQueryService queryService;
    private readonly ISessionService sessionService;

    public DocumentsController(IResourceQueryService queryService, ISessionService sessionService)
    {
        this.queryService = queryService;
        this.sessionService = sessionService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentDescriptor>>> GetDocuments(
        [FromQuery] long printerId,
        [FromQuery] int? limit,
        [FromQuery] long? beforeId,
        [FromQuery] string? sourceIp,
        CancellationToken cancellationToken)
    {
        var context = await GetSessionContextAsync(cancellationToken).ConfigureAwait(false);

        var printer = await queryService.GetPrinterAsync(printerId, cancellationToken).ConfigureAwait(false);
        if (printer is null || !IsPrinterAccessible(printer, context))
        {
            return NotFound();
        }

        var resolvedLimit = limit.GetValueOrDefault(DefaultLimit);
        var query = new ListQuery(resolvedLimit, beforeId, sourceIp);
        var result = await queryService.ListDocumentsAsync(query, cancellationToken).ConfigureAwait(false);

        var filteredItems = result.Items
            .Where(descriptor => descriptor.PrinterId == printerId)
            .ToList();

        var filteredResult = new PagedResult<DocumentDescriptor>(
            filteredItems,
            result.HasMore || filteredItems.Count < result.Items.Count,
            result.NextBeforeId);

        return Ok(filteredResult);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Document>> GetDocument(
        long id,
        [FromQuery] bool includeContent,
        CancellationToken cancellationToken)
    {
        var context = await GetSessionContextAsync(cancellationToken).ConfigureAwait(false);

        var document = await queryService.GetDocumentAsync(id, includeContent, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return NotFound();
        }

        var printer = await queryService.GetPrinterAsync(document.PrinterId, cancellationToken).ConfigureAwait(false);
        if (printer is null || !IsPrinterAccessible(printer, context))
        {
            return NotFound();
        }

        return Ok(document);
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
