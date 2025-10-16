using Printify.Domain.Users;
using Printify.Infrastructure.Persistence.Entities.Users;

namespace Printify.Infrastructure.Mapping;

internal static class UserEntityMapper
{
    internal static UserEntity ToEntity(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserEntity
        {
            Id = user.Id,
            CreatedAt = user.CreatedAt,
            IsDeleted = user.IsDeleted,
            DisplayName = user.DisplayName,
            CreatedFromIp = user.CreatedFromIp
        };
    }

    internal static User ToDomain(this UserEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new User(
            entity.Id,
            entity.DisplayName,
            entity.CreatedAt,
            entity.CreatedFromIp,
            entity.IsDeleted);
    }
}
