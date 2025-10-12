using Printify.Web.Contracts.Users.Responses;

namespace Printify.Web.Contracts.Auth.Responses;

public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    long ExpiresInSeconds,
    UserDto User);
