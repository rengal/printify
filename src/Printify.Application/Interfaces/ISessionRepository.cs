using Printify.Domain.Requests;

namespace Printify.Application.Interfaces;

/// <summary>
/// Provides persistence access to anonymous sessions.
/// </summary>
public interface ISessionRepository
{
    ValueTask<RequestContext> CreateAsync(RequestContext rawContext, CancellationToken cancellationToken);
    ValueTask<RequestContext> AttachUserAsync(RequestContext context, long userId, CancellationToken cancellationToken);
    ValueTask<bool> DeleteAsync(long id, CancellationToken cancellationToken);
    ValueTask LogoutAsync(long sessionId, CancellationToken cancellationToken);
}
