using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Domain.Services;
using Printify.Domain.Sessions;
using Printify.Domain.Users;

namespace Printify.TestServices;

/// <summary>
/// Basic in-memory record storage used by tests to avoid external dependencies.
/// Thread-safe so concurrent scenarios stay deterministic.
/// </summary>
public sealed class InMemoryRecordStorage : IRecordStorage
{
    private readonly object gate = new();
    private readonly List<Document> documents = new();
    private readonly List<User> users = new();
    private readonly List<Printer> printers = new();
    private readonly List<Session> sessions = new();
    private long nextDocumentId = 1;
    private long nextUserId = 1;
    private long nextPrinterId = 1;
    private long nextSessionId = 1;

    public ValueTask<long> AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        Document stored;
        lock (gate)
        {
            // Guard mutation so IDs remain unique and ordering stable.
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
            // Read under the same gate to avoid tearing while enumerations mutate.
            return ValueTask.FromResult<Document?>(documents.FirstOrDefault(d => d.Id == id));
        }
    }

    public ValueTask<IReadOnlyList<Document>> ListDocumentsAsync(
        int limit,
        long? beforeId = null,
        string? sourceIp = null,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        List<Document> snapshot;
        lock (gate)
        {
            // Take a copy so further filtering happens without holding the gate.
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
            // Allocate sequential identifiers to keep tests deterministic.
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

    public ValueTask<User?> GetUserByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            return ValueTask.FromResult<User?>(users.FirstOrDefault(user => string.Equals(user.DisplayName, name, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public ValueTask<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            return ValueTask.FromResult<IReadOnlyList<User>>(users.ToList());
        }
    }

    public ValueTask<bool> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        bool updated;
        lock (gate)
        {
            // Replace the existing entry atomically so readers never observe mixed state.
            var index = users.FindIndex(u => u.Id == user.Id);
            updated = index >= 0;
            if (updated)
            {
                users[index] = user;
            }
        }

        return ValueTask.FromResult(updated);
    }

    public ValueTask<bool> DeleteUserAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool removed;
        lock (gate)
        {
            // Remove all matches to keep state clean even if duplicates are ever inserted.
            removed = users.RemoveAll(u => u.Id == id) > 0;
        }

        return ValueTask.FromResult(removed);
    }

    public ValueTask<long> AddSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        Session stored;
        lock (gate)
        {
            var assignedId = nextSessionId++;
            stored = session with { Id = assignedId };
            sessions.Add(stored);
        }

        return ValueTask.FromResult(stored.Id);
    }

    public ValueTask<Session?> GetSessionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            return ValueTask.FromResult<Session?>(sessions.FirstOrDefault(session => session.Id == id));
        }
    }

    public ValueTask<bool> UpdateSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        bool updated;
        lock (gate)
        {
            var index = sessions.FindIndex(s => s.Id == session.Id);
            updated = index >= 0;
            if (updated)
            {
                sessions[index] = session;
            }
        }

        return ValueTask.FromResult(updated);
    }

    public ValueTask<bool> DeleteSessionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool removed;
        lock (gate)
        {
            removed = sessions.RemoveAll(session => session.Id == id) > 0;
        }

        return ValueTask.FromResult(removed);
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

    public ValueTask<IReadOnlyList<Printer>> ListPrintersAsync(long? ownerUserId = null, long? ownerSessionId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (gate)
        {
            IEnumerable<Printer> query = printers;

            if (ownerUserId is { } userId)
            {
                query = query.Where(printer => printer.OwnerUserId == userId);
            }

            if (ownerSessionId is { } sessionId)
            {
                query = query.Where(printer => printer.OwnerSessionId == sessionId);
            }

            return ValueTask.FromResult<IReadOnlyList<Printer>>(query.ToList());
        }
    }

    public ValueTask<bool> UpdatePrinterAsync(Printer printer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(printer);
        cancellationToken.ThrowIfCancellationRequested();

        bool updated;
        lock (gate)
        {
            // Apply updates atomically so readers never observe half-written printers.
            var index = printers.FindIndex(p => p.Id == printer.Id);
            updated = index >= 0;
            if (updated)
            {
                printers[index] = printer;
            }
        }

        return ValueTask.FromResult(updated);
    }

    public ValueTask<bool> DeletePrinterAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool removed;
        lock (gate)
        {
            // Remove all matches even though IDs are unique to stay defensive.
            removed = printers.RemoveAll(p => p.Id == id) > 0;
        }

        return ValueTask.FromResult(removed);
    }
}
