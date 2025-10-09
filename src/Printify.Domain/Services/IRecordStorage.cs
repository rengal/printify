using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Domain.Sessions;
using Printify.Domain.Users;

namespace Printify.Domain.Services;

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

    ValueTask<User?> GetUserByNameAsync(string name, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> UpdateUserAsync(User user, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteUserAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<long> AddSessionAsync(Session session, CancellationToken cancellationToken = default);

    ValueTask<Session?> GetSessionAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<bool> UpdateSessionAsync(Session session, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteSessionAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<long> AddPrinterAsync(Printer printer, CancellationToken cancellationToken = default);

    ValueTask<Printer?> GetPrinterAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<Printer>> ListPrintersAsync(
        long? ownerUserId = null,
        long? ownerSessionId = null,
        CancellationToken cancellationToken = default);

    ValueTask<bool> UpdatePrinterAsync(Printer printer, CancellationToken cancellationToken = default);

    ValueTask<bool> DeletePrinterAsync(long id, CancellationToken cancellationToken = default);
}
