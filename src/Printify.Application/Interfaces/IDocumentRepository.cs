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
    Task<int> ClearByPrinterIdAsync(Guid printerId, CancellationToken ct);
    Task<long> CountByPrinterIdAsync(Guid printerId, CancellationToken ct);
    Task<long> CountByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct);
    Task<long> CountByWorkspaceIdSinceAsync(Guid workspaceId, DateTimeOffset since, CancellationToken ct);
    Task<DateTimeOffset?> GetLastDocumentTimestampByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct);
    ValueTask<Media?> GetMediaByIdAsync(Guid id, CancellationToken ct);
    ValueTask<Media?> GetMediaByChecksumAsync(string sha256Checksum, Guid? ownerWorkspaceId, CancellationToken ct);
    Task AddMediaAsync(Media media, CancellationToken ct);
}
