using System;

namespace Printify.Web.Contracts.Users.Requests;

/// <summary>
/// Payload required to create a new user.
/// </summary>
/// <param name="Id">Client-generated identifier.</param>
/// <param name="DisplayName">Desired display name.</param>
public sealed record CreateUserRequestDto(Guid Id, string DisplayName);
