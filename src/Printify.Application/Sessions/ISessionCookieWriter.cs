using Printify.Domain.Sessions;

namespace Printify.Application.Sessions;

/// <summary>
/// Abstraction over HTTP cookie handling so the application layer can manage session cookies.
/// </summary>
public interface ISessionCookieWriter
{
    void SetSessionCookie(long sessionId, DateTimeOffset expiresAt);

    void ClearSessionCookie();
}
