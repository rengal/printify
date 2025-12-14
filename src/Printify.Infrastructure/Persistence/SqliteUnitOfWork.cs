using Microsoft.Extensions.Logging;
using Printify.Application.Interfaces;

namespace Printify.Infrastructure.Persistence;

public sealed class SqliteUnitOfWork(
    PrintifyDbContext dbContext,
    ILogger<SqliteUnitOfWork> logger)
    : IUnitOfWork
{
    private static readonly SemaphoreSlim TransactionGate = new(1, 1);
    private bool ownsLock;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        await TransactionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        ownsLock = true;
        await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        logger.LogDebug("SQLite transaction started.");
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug("SQLite transaction committed.");
        }
        finally
        {
            if (ownsLock)
            {
                TransactionGate.Release();
                ownsLock = false;
            }
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);
            logger.LogWarning("SQLite transaction rolled back.");
        }
        finally
        {
            if (ownsLock)
            {
                TransactionGate.Release();
                ownsLock = false;
            }
        }
    }
}
