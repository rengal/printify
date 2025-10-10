namespace Printify.Domain.Sessions;

/// <summary>
/// Shared constants controlling session behavior across layers.
/// </summary>
public static class SessionDefaults
{
    /// <summary>
    /// Name of the HTTP cookie carrying the session identifier.
    /// </summary>
    public const string SessionCookieName = "session_id";

    /// <summary>
    /// Lifetime applied to sessions when refreshed.
    /// </summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);
}
