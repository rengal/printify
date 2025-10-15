using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.AnonymousSessions;

namespace Printify.Application.Features.Auth.CreateAnonymousSession;

public sealed class CreateAnonymousSessionHandler(IAnonymousSessionRepository sessionRepository)
    : IRequestHandler<CreateAnonymousSessionCommand, AnonymousSession>
{
    public async Task<AnonymousSession> Handle(
        CreateAnonymousSessionCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Anonymous sessions require a traceable client address for auditing and matching.
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Context.IpAddress);

        // Generate a new anonymous session using the request context to capture the caller's origin.
        var session = AnonymousSession.Create(request.Context.IpAddress);

        // Persist the session so the caller receives a durable identifier for subsequent requests.
        return await sessionRepository.AddAsync(session, ct);
    }
}
