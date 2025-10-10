namespace Printify.Web.Contracts.Users.Requests;

/// <summary>
/// Payload required to create a new user.
/// </summary>
/// <param name="DisplayName">Desired display name.</param>
public sealed record CreateUserRequest(string DisplayName);
