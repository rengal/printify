namespace Printify.Web.Contracts.Auth.AnonymousSession.Response;

public sealed record AnonymousSessionDto(
    Guid Id,
    DateTimeOffset CreatedAt);
