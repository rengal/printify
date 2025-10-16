using Printify.Domain.AnonymousSessions;
using Printify.Infrastructure.Persistence.Entities.AnonymousSessions;

namespace Printify.Infrastructure.Mapping;

internal static class AnonymousSessionEntityMapper
{
    internal static AnonymousSessionEntity ToEntity(this AnonymousSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new AnonymousSessionEntity
        {
            Id = session.Id,
            CreatedAt = session.CreatedAt,
            LastActiveAt = session.LastActiveAt,
            CreatedFromIp = session.CreatedFromIp,
            LinkedUserId = session.LinkedUserId,
            IsDeleted = session.IsDeleted
        };
    }

    internal static AnonymousSession ToDomain(this AnonymousSessionEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AnonymousSession(
            entity.Id,
            entity.CreatedAt,
            entity.LastActiveAt,
            entity.CreatedFromIp,
            entity.LinkedUserId,
            entity.IsDeleted);
    }
}

