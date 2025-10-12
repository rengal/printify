namespace Printify.Domain.Requests;

/// <summary>
/// Contextual information captured for a single request, including the resolved session.
/// </summary>
/// <param name="AnonymousSessionId">Business anonymous session identifier (not equal to browser cookies).</param>
/// <param name="UserId">Optional identifier of the authenticated user tied to the session.</param>
/// <param name="IpAddress">Client IP address observed for the request.</param>
/// <param name="IdempotencyKey">Optional idempotency key supplied by the caller.</param>
public sealed record RequestContext(
    Guid? AnonymousSessionId,
    Guid? UserId,
    string IpAddress,
    string? IdempotencyKey);
