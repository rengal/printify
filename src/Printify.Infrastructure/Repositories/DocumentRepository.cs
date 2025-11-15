using System.Collections.Concurrent;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;

namespace Printify.Infrastructure.Repositories;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, Document> documents = new();

    public ValueTask<Document?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        documents.TryGetValue(id, out var document);
        return ValueTask.FromResult(document);
    }

    public Task<IReadOnlyList<Document>> ListByPrinterIdAsync(
        Guid printerId,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var query = documents.Values
            .Where(document => document.PrinterId == printerId);

        if (from.HasValue)
        {
            query = query.Where(document => document.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(document => document.CreatedAt <= to.Value);
        }

        if (beforeCreatedAt.HasValue)
        {
            query = query.Where(document =>
                document.CreatedAt < beforeCreatedAt.Value ||
                (document.CreatedAt == beforeCreatedAt.Value && beforeId.HasValue && document.Id.CompareTo(beforeId.Value) < 0));
        }

        var ordered = query
            .OrderByDescending(document => document.CreatedAt)
            .ThenByDescending(document => document.Id)
            .Take(limit <= 0 ? 20 : Math.Min(limit, 200))
            .ToArray()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Document>>(ordered);
    }

    public Task AddAsync(Document document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        ct.ThrowIfCancellationRequested();
        documents[document.Id] = document;
        return Task.CompletedTask;
    }
}
