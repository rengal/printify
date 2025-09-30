using Printify.Contracts;
using Printify.Contracts.Service;

namespace Printify.TestServices;

/// <summary>
/// Simple in-memory storage used for tests; not intended for production workloads.
/// </summary>
public sealed class InMemoryRecordStorage : IRecordStorage
{
    private readonly object syncRoot = new();
    private readonly List<Document> documents = new();
    private long nextId = 1;

    public ValueTask<long> AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Document stored;
        lock (syncRoot)
        {
            // Serialize access so ids stay strictly increasing.
            var assignedId = nextId++;
            stored = document with { Id = assignedId };
            documents.Add(stored);
        }

        return ValueTask.FromResult(stored.Id);
    }

    public ValueTask<Document?> GetDocumentAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Document? match;
        lock (syncRoot)
        {
            // Documents are small; linear scan keeps implementation trivial for tests.
            match = documents.FirstOrDefault(d => d.Id == id);
        }

        return ValueTask.FromResult(match);
    }

    public ValueTask<IReadOnlyList<Document>> ListDocumentsAsync(
        int limit,
        long? beforeId = null,
        string? sourceIp = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be greater than zero.");
        }

        List<Document> snapshot;
        lock (syncRoot)
        {
            // Take a snapshot to avoid exposing internal list to callers.
            snapshot = documents.ToList();
        }

        IEnumerable<Document> query = snapshot;

        if (!string.IsNullOrWhiteSpace(sourceIp))
        {
            // Case-insensitive comparison aligns with production filtering expectations.
            query = query.Where(d => string.Equals(d.SourceIp, sourceIp, StringComparison.OrdinalIgnoreCase));
        }

        if (beforeId.HasValue)
        {
            query = query.Where(d => d.Id < beforeId.Value);
        }

        query = query
            .OrderByDescending(d => d.Id)
            .Take(limit);

        var result = query.ToList();
        return ValueTask.FromResult<IReadOnlyList<Document>>(result);
    }
}
