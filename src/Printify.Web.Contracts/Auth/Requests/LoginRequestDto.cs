namespace Printify.Web.Contracts.Auth.Requests;

/// <summary>
/// Payload used to initiate a login session for an existing or new user.
/// </summary>
/// <param name="DisplayName">Desired display name supplied by the caller.</param>
public sealed record LoginRequestDto(string DisplayName);
