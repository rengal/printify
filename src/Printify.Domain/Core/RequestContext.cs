namespace Printify.Domain.Core;

public sealed record RequestContext(string SessionId, string IdempotencyKey, string Ip);
