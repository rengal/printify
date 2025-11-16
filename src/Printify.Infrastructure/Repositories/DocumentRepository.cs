using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Infrastructure.Documents;

namespace Printify.Infrastructure.Repositories;

/// <summary>
/// TODO: Persist schema version for documents once element agreements evolve.
/// TODO: Replace in-memory storage with DbContext-backed persistence.
/// </summary>
public sealed class DocumentRepository : IDocumentRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentDictionary<Guid, string> documents = new();

    public ValueTask<Document?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!documents.TryGetValue(id, out var payload))
        {
            return ValueTask.FromResult<Document?>(null);
        }

        var persisted = JsonSerializer.Deserialize<PersistedDocument>(payload, SerializerOptions);
        return ValueTask.FromResult(persisted?.ToDomain());
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

        var results = documents.Values
            .Select(payload => JsonSerializer.Deserialize<PersistedDocument>(payload, SerializerOptions))
            .Where(persisted => persisted is not null && persisted.PrinterId == printerId)
            .Select(persisted => persisted!)
            .Where(persisted =>
                (!from.HasValue || persisted.CreatedAt >= from.Value) &&
                (!to.HasValue || persisted.CreatedAt <= to.Value))
            .Where(persisted => beforeCreatedAt is null ||
                persisted.CreatedAt < beforeCreatedAt.Value ||
                (persisted.CreatedAt == beforeCreatedAt.Value && beforeId.HasValue && persisted.Id.CompareTo(beforeId.Value) < 0))
            .OrderByDescending(persisted => persisted.CreatedAt)
            .ThenByDescending(persisted => persisted.Id)
            .Take(limit <= 0 ? 20 : Math.Min(limit, 200))
            .Select(persisted => persisted.ToDomain())
            .Where(document => document is not null)
            .Select(document => document!)
            .ToArray()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Document>>(results);
    }

    public Task AddAsync(Document document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        ct.ThrowIfCancellationRequested();
        var persisted = PersistedDocument.FromDomain(document);
        documents[document.Id] = JsonSerializer.Serialize(persisted, SerializerOptions);
        return Task.CompletedTask;
    }

    private sealed record PersistedDocument(
        Guid Id,
        Guid PrintJobId,
        Guid PrinterId,
        DateTimeOffset CreatedAt,
        Protocol Protocol,
        string? ClientAddress,
        List<DocumentElementDto> Elements)
    {
        public static PersistedDocument FromDomain(Document document)
        {
            return new PersistedDocument(
                document.Id,
                document.PrintJobId,
                document.PrinterId,
                document.CreatedAt,
                document.Protocol,
                document.ClientAddress,
                document.Elements.Select(DocumentElementMapper.ToDto).ToList());
        }

        public Document ToDomain()
        {
            var items = Elements ?? new List<DocumentElementDto>();
            var domainElements = items.Select(DocumentElementMapper.ToDomain).ToArray();
            return new Document(
                Id,
                PrintJobId,
                PrinterId,
                CreatedAt,
                Protocol,
                ClientAddress,
                domainElements);
        }
    }
}
