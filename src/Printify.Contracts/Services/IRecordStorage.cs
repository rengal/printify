using Printify.Contracts.Documents;
using Printify.Contracts.Printers;
using Printify.Contracts.Users;

namespace Printify.Contracts.Services;

/// <summary>
/// Storage abstraction for persisting and retrieving domain data.
/// </summary>
public interface IRecordStorage
{
    ValueTask<long> AddDocumentAsync(Document document, CancellationToken cancellationToken = default);

    ValueTask<Document?> GetDocumentAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<Document>> ListDocumentsAsync(
        int limit,
        long? beforeId = null,
        string? sourceIp = null,
        CancellationToken cancellationToken = default);

    ValueTask<long> AddUserAsync(User user, CancellationToken cancellationToken = default);

    ValueTask<User?> GetUserAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<bool> UpdateUserAsync(User user, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteUserAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<long> AddPrinterAsync(Printer printer, CancellationToken cancellationToken = default);

    ValueTask<Printer?> GetPrinterAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<bool> UpdatePrinterAsync(Printer printer, CancellationToken cancellationToken = default);

    ValueTask<bool> DeletePrinterAsync(long id, CancellationToken cancellationToken = default);
}
