using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Queries;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.Contracts.Users;
using Printify.Web.Security;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private const int DefaultLimit = 20;
    private readonly IResourceQueryService queryService;

    public DocumentsController(IResourceQueryService queryService)
    {
        this.queryService = queryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentDescriptor>>> GetDocuments(
        [FromQuery] long printerId,
        [FromQuery] int? limit,
        [FromQuery] long? beforeId,
        [FromQuery] string? sourceIp,
        CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync(cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Unauthorized();
        }

        var printer = await EnsureOwnedPrinterAsync(printerId, user, cancellationToken).ConfigureAwait(false);
        if (printer is null)
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
        var user = await GetAuthenticatedUserAsync(cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Unauthorized();
        }

        var document = await queryService.GetDocumentAsync(id, includeContent, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return NotFound();
        }

        var printer = await EnsureOwnedPrinterAsync(document.PrinterId, user, cancellationToken).ConfigureAwait(false);
        if (printer is null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    private async Task<User?> GetAuthenticatedUserAsync(CancellationToken cancellationToken)
    {
        if (!TokenService.TryExtractUsername(HttpContext, out var username))
        {
            return null;
        }

        return await queryService.FindUserByNameAsync(username, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Printer?> EnsureOwnedPrinterAsync(long printerId, User user, CancellationToken cancellationToken)
    {
        var printer = await queryService.GetPrinterAsync(printerId, cancellationToken).ConfigureAwait(false);
        if (printer is null || printer.OwnerUserId != user.Id)
        {
            return null;
        }

        return printer;
    }
}