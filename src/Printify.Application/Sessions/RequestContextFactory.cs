using System;
using Printify.Domain.Requests;
using Printify.Domain.Sessions;
using Printify.Domain.Services;

namespace Printify.Application.Sessions;

/// <inheritdoc />
public sealed class RequestContextFactory : IRequestContextFactory
{
    private readonly ISessionService sessionService;
    private readonly ISessionCookieWriter cookieWriter;

    public RequestContextFactory(ISessionService sessionService, ISessionCookieWriter cookieWriter)
    {
        ArgumentNullException.ThrowIfNull(sessionService);
        ArgumentNullException.ThrowIfNull(cookieWriter);

        this.sessionService = sessionService;
        this.cookieWriter = cookieWriter;
    }

    public async ValueTask<RequestContext> CreateAsync(RequestContext rawContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rawContext);

        var now = DateTimeOffset.UtcNow;
        Session? session = null;

        if (rawContext.SessionId is { } sessionId)
        {
            session = await sessionService.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (session is not null && session.ExpiresAt <= now)
            {
                await sessionService.DeleteAsync(session.Id, cancellationToken).ConfigureAwait(false);
                session = null;
            }
        }

        if (session is null)
        {
            session = await sessionService.CreateAsync(rawContext.IpAddress, now, now.Add(SessionDefaults.SessionLifetime), cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            session = session with
            {
                LastActiveAt = now,
                ExpiresAt = now.Add(SessionDefaults.SessionLifetime)
            };

            await sessionService.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
        }

        cookieWriter.SetSessionCookie(session.Id, session.ExpiresAt);

        return new RequestContext(
            session.Id,
            session.ClaimedUserId,
            rawContext.IpAddress,
            rawContext.IdempotencyKey,
            session.ExpiresAt);
    }

    public async ValueTask<RequestContext> AttachUserAsync(RequestContext context, long userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), userId, "User identifier must be positive.");
        }

        if (context.SessionId is null)
        {
            throw new InvalidOperationException("Session must be resolved before attaching a user.");
        }

        var session = await sessionService.GetAsync(context.SessionId.Value, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new InvalidOperationException("Session no longer exists.");
        }

        var now = DateTimeOffset.UtcNow;
        session = session with
        {
            ClaimedUserId = userId,
            LastActiveAt = now,
            ExpiresAt = now.Add(SessionDefaults.SessionLifetime)
        };

        await sessionService.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
        cookieWriter.SetSessionCookie(session.Id, session.ExpiresAt);

        return new RequestContext(
            session.Id,
            session.ClaimedUserId,
            context.IpAddress,
            context.IdempotencyKey,
            session.ExpiresAt);
    }

    public async ValueTask LogoutAsync(long sessionId, CancellationToken cancellationToken)
    {
        if (sessionId <= 0)
        {
            return;
        }

        await sessionService.DeleteAsync(sessionId, cancellationToken).ConfigureAwait(false);
        cookieWriter.ClearSessionCookie();
    }
}
