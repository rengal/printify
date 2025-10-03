using Printify.Contracts.Documents;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.Contracts.Users;

namespace Printify.TestServices;

/// <summary>
/// Simple in-memory storage used for tests; not intended for production workloads.
/// </summary>
public sealed class InMemoryRecordStorage : IRecordStorage
{
    private readonly object syncRoot = new();
    private readonly List<Document> documents = new();
    private readonly List<User> users = new();
    private readonly List<Printer> printers = new();
    private long nextDocumentId = 1;
    private long nextUserId = 1;
    private long nextPrinterId = 1;

    public ValueTask<long> AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Document stored;
        lock (syncRoot)
        {
            var assignedId = nextDocumentId++;
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
            snapshot = documents.ToList();
        }

        IEnumerable<Document> query = snapshot;

        if (!string.IsNullOrWhiteSpace(sourceIp))
        {
            query = query.Where(d => string.Equals(d.SourceIp, sourceIp, StringComparison.OrdinalIgnoreCase));
        }

        if (beforeId.HasValue)
        {
            query = query.Where(d => d.Id < beforeId.Value);
        }

        var result = query
            .OrderByDescending(d => d.Id)
            .Take(limit)
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<Document>>(result);
    }

    public ValueTask<long> AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        User stored;
        lock (syncRoot)
        {
            var assignedId = nextUserId++;
            stored = user with { Id = assignedId };
            users.Add(stored);
        }

        return ValueTask.FromResult(stored.Id);
    }

    public ValueTask<User?> GetUserAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        User? match;
        lock (syncRoot)
        {
            match = users.FirstOrDefault(u => u.Id == id);
        }

        return ValueTask.FromResult(match);
    }

    public ValueTask<long> AddPrinterAsync(Printer printer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Printer stored;
        lock (syncRoot)
        {
            var assignedId = nextPrinterId++;
            stored = printer with { Id = assignedId };
            printers.Add(stored);
        }

        return ValueTask.FromResult(stored.Id);
    }

    public ValueTask<Printer?> GetPrinterAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Printer? match;
        lock (syncRoot)
        {
            match = printers.FirstOrDefault(p => p.Id == id);
        }

        return ValueTask.FromResult(match);
    }
}
