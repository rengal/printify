using Microsoft.AspNetCore.Mvc;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Queries;
using Printify.Contracts.Documents.Services;

namespace Printify.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private const int DefaultLimit = 20;
    private readonly IResouceQueryService queryService;

    public DocumentsController(IResouceQueryService queryService)
    {
        this.queryService = queryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentDescriptor>>> ListAsync(
        [FromQuery] int? limit,
        [FromQuery] long? beforeId,
        [FromQuery] string? sourceIp,
        CancellationToken cancellationToken)
    {
        var resolvedLimit = limit.GetValueOrDefault(DefaultLimit);
        var query = new ListQuery(resolvedLimit, beforeId, sourceIp);
        var result = await queryService.ListAsync(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Document>> GetAsync(
        long id,
        [FromQuery] bool includeContent,
        CancellationToken cancellationToken)
    {
        var document = await queryService.GetAsync(id, includeContent, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return NotFound();
        }

        return Ok(document);
    }
}
