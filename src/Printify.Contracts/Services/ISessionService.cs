using Printify.Contracts.Sessions;

namespace Printify.Contracts.Services;

/// <summary>
/// Provides persistence access to anonymous sessions.
/// </summary>
public interface ISessionService
{
    ValueTask<Session> CreateAsync(string createdFromIp, DateTimeOffset createdAt, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    ValueTask<Session?> GetAsync(long id, CancellationToken cancellationToken = default);

    ValueTask<bool> UpdateAsync(Session session, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
