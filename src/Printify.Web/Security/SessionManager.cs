using System.Globalization;
using Microsoft.AspNetCore.Http;
using Printify.Domain.Services;
using Printify.Domain.Sessions;

namespace Printify.Web.Security;

internal static class SessionManager
{
    internal const string SessionCookieName = "session_id";
    internal static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

    internal static async ValueTask<Session> GetOrCreateSessionAsync(HttpContext context, ISessionService sessionService, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        Session? session = null;

        if (context.Request.Cookies.TryGetValue(SessionCookieName, out var cookieValue) &&
            long.TryParse(cookieValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sessionId))
        {
            session = await sessionService.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (session is not null && session.ExpiresAt > now)
            {
                session = session with { LastActiveAt = now, ExpiresAt = now.Add(SessionLifetime) };
                await sessionService.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
            }
            else if (session is not null)
            {
                await sessionService.DeleteAsync(session.Id, cancellationToken).ConfigureAwait(false);
                session = null;
            }
        }

        if (session is null)
        {
            var createdFromIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            session = await sessionService.CreateAsync(createdFromIp, now, now.Add(SessionLifetime), cancellationToken).ConfigureAwait(false);
        }

        context.Response.Cookies.Append(
            SessionCookieName,
            session.Id.ToString(CultureInfo.InvariantCulture),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                MaxAge = SessionLifetime,
                Path = "/"
            });

        return session;
    }

    internal static void ClearSessionCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(SessionCookieName);
    }
}

