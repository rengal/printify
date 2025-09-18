using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Printify.Contracts.Service;

using Printify.Contracts;

/// <summary>
/// Storage abstraction for persisting and retrieving domain data.
/// Start with documents; can expand to other artifacts later.
/// </summary>
public interface IRecordStorage
{
    /// <summary>
    /// Adds a single parsed document to storage and returns its generated identifier.
    /// </summary>
    /// <param name="document">Semantic document (elements, protocol, timestamp). Id should be 0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated document identifier.</returns>
    ValueTask<long> AddDocumentAsync(Document document, CancellationToken cancellationToken = default);

    // Raw bytes are intentionally not stored here; upstream can persist them separately if needed.

    /// <summary>
    /// Gets a single document by its identifier, or null if not found.
    /// </summary>
    ValueTask<Document?> GetDocumentAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the newest documents ordered by id descending.
    /// </summary>
    /// <param name="limit">Maximum number of documents to return.</param>
    /// <param name="beforeId">Exclusive upper bound for id when requesting older items.</param>
    /// <param name="sourceIp">Optional filter by source IP.</param>
    ValueTask<IReadOnlyList<Document>> ListDocumentsAsync(
        int limit,
        long? beforeId = null,
        string? sourceIp = null,
        CancellationToken cancellationToken = default);
}
