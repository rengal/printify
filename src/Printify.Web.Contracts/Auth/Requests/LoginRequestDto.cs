namespace Printify.Web.Contracts.Auth.Requests;

/// <summary>
/// Payload used to initiate a login session for an existing workspace.
/// </summary>
public sealed record LoginRequestDto(string Token);
