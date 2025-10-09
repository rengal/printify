namespace Printify.Domain.Users;

/// <summary>
/// Payload required to create a new user.
/// </summary>
/// <param name="DisplayName">Desired display name.</param>
/// <param name="CreatedFromIp">IP address captured at registration time.</param>
public sealed record SaveUserRequest(
    string DisplayName,
    string CreatedFromIp);
