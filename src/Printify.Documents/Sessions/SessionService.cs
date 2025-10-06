using Printify.Contracts.Sessions;
using Printify.Contracts.Services;

namespace Printify.Documents.Sessions;

public sealed class SessionService : ISessionService
{
    private readonly IRecordStorage recordStorage;

    public SessionService(IRecordStorage recordStorage)
    {
        this.recordStorage = recordStorage;
    }

    public async ValueTask<Session> CreateAsync(string createdFromIp, DateTimeOffset createdAt, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var session = new Session(0, createdAt, createdAt, createdFromIp, null, expiresAt);
        var id = await recordStorage.AddSessionAsync(session, cancellationToken).ConfigureAwait(false);
        return session with { Id = id };
    }

    public ValueTask<Session?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        return recordStorage.GetSessionAsync(id, cancellationToken);
    }

    public ValueTask<bool> UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        return recordStorage.UpdateSessionAsync(session, cancellationToken);
    }

    public ValueTask<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        return recordStorage.DeleteSessionAsync(id, cancellationToken);
    }
}
