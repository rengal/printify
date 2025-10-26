using Microsoft.Extensions.Logging;
using Printify.Application.Interfaces;

namespace Printify.Infrastructure.Persistence;

public sealed class SqliteUnitOfWork(
    PrintifyDbContext dbContext,
    ILogger<SqliteUnitOfWork> logger)
    : IUnitOfWork
{
    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        logger.LogDebug("SQLite transaction started.");
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        logger.LogDebug("SQLite transaction committed.");
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        logger.LogWarning("SQLite transaction rolled back.");
    }
}
