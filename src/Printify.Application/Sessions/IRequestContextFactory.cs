using Printify.Domain.Requests;

namespace Printify.Application.Sessions;

/// <summary>
/// Resolves the current session and produces a request context enriched with session details.
/// </summary>
public interface IRequestContextFactory
{
    ValueTask<RequestContext> CreateAsync(RequestContext rawContext, CancellationToken cancellationToken);

    ValueTask<RequestContext> AttachUserAsync(RequestContext context, long userId, CancellationToken cancellationToken);

    ValueTask LogoutAsync(long sessionId, CancellationToken cancellationToken);
}
