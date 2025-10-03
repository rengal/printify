namespace Printify.Contracts.Core;

/// <summary>
/// Carries cross-cutting request metadata such as idempotency key.
/// </summary>
/// <param name="IdempotencyKey">Stable key supplied by the client to de-duplicate retries.</param>
public sealed record RequestContext(string IdempotencyKey);
