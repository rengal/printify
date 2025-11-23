using Printify.Domain.Documents;
using Printify.Domain.Media;

namespace Printify.Application.Interfaces;

public interface IDocumentRepository
{
    ValueTask<Document?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Document>> ListByPrinterIdAsync(
        Guid printerId,
        Guid? beforeId,
        int limit,
        CancellationToken ct);
    Task AddAsync(Document document, CancellationToken ct);
    Task<long> CountByPrinterIdAsync(Guid printerId, CancellationToken ct);
    ValueTask<Media?> GetMediaByIdAsync(Guid id, CancellationToken ct);
    ValueTask<Media?> GetMediaByChecksumAsync(string checksum, CancellationToken ct);
}
