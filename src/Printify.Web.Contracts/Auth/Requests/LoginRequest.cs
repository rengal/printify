namespace Printify.Web.Contracts.Auth.Requests;

/// <summary>
/// Payload used to initiate a login session for an existing or new user.
/// </summary>
/// <param name="Username">Desired display name supplied by the caller.</param>
public sealed record LoginRequest(string Username);
