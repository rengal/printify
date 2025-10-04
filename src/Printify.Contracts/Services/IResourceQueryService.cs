using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Queries;
using Printify.Contracts.Printers;
using Printify.Contracts.Users;

namespace Printify.Contracts.Services;

/// <summary>
/// Query-side service that retrieves document metadata and related resources from storage.
/// </summary>
public interface IResourceQueryService
{
    ValueTask<PagedResult<DocumentDescriptor>> ListDocumentsAsync(ListQuery query, CancellationToken cancellationToken = default);

    ValueTask<Document?> GetDocumentAsync(long id, bool includeContent = false, CancellationToken cancellationToken = default);

    ValueTask<User?> GetUserAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<Printer?> GetPrinterAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<Printer>> ListPrintersAsync(long? ownerUserId = null, CancellationToken cancellationToken = default);
}