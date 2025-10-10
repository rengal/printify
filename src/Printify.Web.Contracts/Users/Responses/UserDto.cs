namespace Printify.Web.Contracts.Users.Responses;

/// <summary>
/// Lightweight user projection returned by the API.
/// </summary>
/// <param name="Id">Identifier of the user.</param>
/// <param name="Name">Display name chosen by the user.</param>
public sealed record UserDto(long Id, string Name);
