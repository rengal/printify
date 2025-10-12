using Printify.Domain.AnonymousSessions;
using Printify.Domain.Requests;

namespace Printify.Application.Interfaces;

public interface IAnonymousSessionRepository
{
    ValueTask<AnonymousSession?> GetByIdAsync(Guid id, CancellationToken ct);
    ValueTask<AnonymousSession> AddAsync(AnonymousSession session, CancellationToken ct);
    Task UpdateAsync(AnonymousSession session, CancellationToken ct);

    // Specialized shortcuts for performance
    Task TouchAsync(Guid id, DateTimeOffset lastActive, CancellationToken ct);
    Task AttachUserAsync(Guid id, Guid userId, CancellationToken ct);
}
