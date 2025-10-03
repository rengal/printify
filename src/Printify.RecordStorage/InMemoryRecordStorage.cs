using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Printify.Contracts.Documents;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.Contracts.Users;

namespace Printify.RecordStorage;

/// <summary>
/// Basic in-memory record storage used until a persistent provider is wired in.
/// Thread-safe so it can back the web API during early iterations.
/// </summary>
public sealed class InMemoryRecordStorage : IRecordStorage
{
    private readonly object gate = new();
    private readonly List<Document> documents = new();
    private readonly List<User> users = new();
    private readonly List<Printer> printers = new();
    private long nextDocumentId = 1;
    private long nextUserId = 1;
    private long nextPrinterId = 1;

    public ValueTask<long> AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        Document stored;
        lock (gate)
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

        lock (gate)
        {
            return ValueTask.FromResult<Document?>(documents.FirstOrDefault(d => d.Id == id));
        }
    }

    public ValueTask<IReadOnlyList<Document>> ListDocumentsAsync(int limit, long? beforeId = null, string? sourceIp = null, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        List<Document> snapshot;
        lock (gate)
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
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        User stored;
        lock (gate)
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

        lock (gate)
        {
            return ValueTask.FromResult<User?>(users.FirstOrDefault(u => u.Id == id));
        }
    }

    public ValueTask<long> AddPrinterAsync(Printer printer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(printer);
        cancellationToken.ThrowIfCancellationRequested();

        Printer stored;
        lock (gate)
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

        lock (gate)
        {
            return ValueTask.FromResult<Printer?>(printers.FirstOrDefault(p => p.Id == id));
        }
    }
}
