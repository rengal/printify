namespace Printify.Web.Contracts.Users;

/// <summary>
/// Payload required to create a new user.
/// </summary>
/// <param name="DisplayName">Desired display name.</param>
public sealed record SaveUserRequest(
    string DisplayName);
