using Printify.Domain.Users;
using Printify.Web.Contracts.Users.Responses;

namespace Printify.Web.Mapping;

internal static class UserMapper
{
    internal static UserDto ToDto(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserDto(user.Id, user.DisplayName);
    }

    internal static IReadOnlyList<UserDto> ToDtos(this IEnumerable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);
        return users.Select(ToDto).ToList();
    }
}
