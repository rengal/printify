using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Printify.Application.Interfaces;
using Printify.Domain.Users;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence;

namespace Printify.Infrastructure.Repositories;

public sealed class UserRepository(PrintifyDbContext dbContext) : IUserRepository
{
    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var entity = user.ToEntity();
        await dbContext.Users.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<User?> GetByDisplayNameAsync(string displayName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var entity = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.DisplayName == displayName && !user.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async ValueTask<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id && !user.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<User>> ListActiveAsync(CancellationToken cancellationToken)
    {
        // Only non-deleted users participate in API results to avoid exposing soft-deleted records.
        var entities = await dbContext.Users
            .AsNoTracking()
            .Where(user => !user.IsDeleted)
            .OrderBy(user => user.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities
            .Select(UserEntityMapper.ToDomain)
            .ToList();
    }
}
