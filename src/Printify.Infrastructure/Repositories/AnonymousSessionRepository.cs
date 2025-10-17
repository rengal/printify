using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Printify.Application.Interfaces;
using Printify.Domain.AnonymousSessions;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence;

namespace Printify.Infrastructure.Repositories;

public sealed class AnonymousSessionRepository(
    PrintifyDbContext dbContext,
    ILogger<AnonymousSessionRepository> logger)
    : IAnonymousSessionRepository
{
    public async ValueTask<AnonymousSession?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await dbContext.AnonymousSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(session => session.Id == id && !session.IsDeleted, ct)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async ValueTask<AnonymousSession> AddAsync(AnonymousSession session, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);

        var entity = session.ToEntity();
        await dbContext.AnonymousSessions.AddAsync(entity, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogDebug("Anonymous session {SessionId} created.", session.Id);
        return session;
    }

    public async Task UpdateAsync(AnonymousSession session, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);

        var entity = await dbContext.AnonymousSessions
            .FirstOrDefaultAsync(s => s.Id == session.Id && !s.IsDeleted, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new InvalidOperationException($"Session {session.Id} does not exist.");
        }

        entity.CreatedAt = session.CreatedAt;
        entity.LastActiveAt = session.LastActiveAt;
        entity.CreatedFromIp = session.CreatedFromIp;
        entity.LinkedUserId = session.LinkedUserId;
        entity.IsDeleted = session.IsDeleted;

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task TouchAsync(Guid id, DateTimeOffset lastActive, CancellationToken ct)
    {
        var affected = await dbContext.AnonymousSessions
            .Where(session => session.Id == id && !session.IsDeleted)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.LastActiveAt, lastActive), ct)
            .ConfigureAwait(false);

        if (affected == 0)
        {
            throw new InvalidOperationException($"Unable to update last activity timestamp for anonymous session {id}.");
        }
    }

    public async Task AttachUserAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var affected = await dbContext.AnonymousSessions
            .Where(session => session.Id == id && !session.IsDeleted)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.LinkedUserId, userId), ct)
            .ConfigureAwait(false);

        if (affected == 0)
        {
            throw new InvalidOperationException($"Failed to attach user {userId} to anonymous session {id}.");
        }
    }
}
