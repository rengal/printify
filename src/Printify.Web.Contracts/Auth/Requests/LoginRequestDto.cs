namespace Printify.Web.Contracts.Auth.Requests;

/// <summary>
/// Payload used to initiate a login session for an existing or new user.
/// </summary>
public sealed record LoginRequestDto(Guid UserId);
