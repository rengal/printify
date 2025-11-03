using Printify.Domain.Documents;

namespace Printify.Application.Interfaces;

public interface IDocumentRepository
{
    ValueTask<Document?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Document document, CancellationToken ct);
}
