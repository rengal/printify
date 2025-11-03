using Printify.Application.Interfaces;
using Printify.Domain.Documents;

namespace Printify.Infrastructure.Repositories;

public sealed class DocumentRepository : IDocumentRepository
{
    public async ValueTask<Document?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return null;
    }

    public async Task AddAsync(Document document, CancellationToken ct)
    {

    }
}
