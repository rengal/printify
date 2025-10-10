namespace Printify.Domain.Requests;

/// <summary>
/// Contextual information captured for a single request, including the resolved session.
/// </summary>
/// <param name="SessionId">Identifier of the active session.</param>
/// <param name="UserId">Optional identifier of the authenticated user tied to the session.</param>
/// <param name="IpAddress">Client IP address observed for the request.</param>
/// <param name="IdempotencyKey">Optional idempotency key supplied by the caller.</param>
/// <param name="SessionExpiresAt">Timestamp when the session should expire.</param>
public sealed record RequestContext(
    Guid SessionId,
    long? UserId,
    string IpAddress,
    string? IdempotencyKey,
    DateTimeOffset? SessionExpiresAt);
