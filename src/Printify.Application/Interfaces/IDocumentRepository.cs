using Printify.Domain.Documents;

namespace Printify.Application.Interfaces;

public interface IDocumentRepository
{
    ValueTask<Document?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Document>> ListByPrinterIdAsync(
        Guid printerId,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken ct);
    Task AddAsync(Document document, CancellationToken ct);
}
