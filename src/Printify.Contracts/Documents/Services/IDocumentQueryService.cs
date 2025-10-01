using System.Threading;
using System.Threading.Tasks;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Queries;

namespace Printify.Contracts.Documents.Services;

/// <summary>
/// Query-side service that retrieves document metadata and full payloads from storage.
/// </summary>
public interface IDocumentQueryService
{
    /// <summary>
    /// Lists documents using cursor-based pagination.
    /// </summary>
    ValueTask<PagedResult<DocumentDescriptor>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single document by identifier.
    /// </summary>
    /// <param name="id">Document identifier.</param>
    /// <param name="includeContent">Controls whether raster image bytes should be hydrated.</param>
    ValueTask<Document?> GetAsync(long id, bool includeContent = false, CancellationToken cancellationToken = default);
}
