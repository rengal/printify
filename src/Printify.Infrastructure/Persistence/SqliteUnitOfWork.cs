using Microsoft.Extensions.Logging;
using Printify.Application.Interfaces;

namespace Printify.Infrastructure.Persistence;

public sealed class SqliteUnitOfWork(
    SqliteConnectionManager connectionManager,
    ILogger<SqliteUnitOfWork> logger)
    : IUnitOfWork
{
    private SqliteTransactionScope? activeScope;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (activeScope is not null)
        {
            throw new InvalidOperationException("A transaction is already active for this unit of work.");
        }

        // Ask the connection manager for a shared connection so repositories participate in the same transaction.
        activeScope = await connectionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        logger.LogDebug("SQLite transaction started.");
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (activeScope is null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await activeScope.Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug("SQLite transaction committed.");
        }
        finally
        {
            await DisposeScopeAsync().ConfigureAwait(false);
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (activeScope is null)
        {
            throw new InvalidOperationException("No active transaction to roll back.");
        }

        try
        {
            await activeScope.Transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            logger.LogWarning("SQLite transaction rolled back.");
        }
        finally
        {
            await DisposeScopeAsync().ConfigureAwait(false);
        }
    }

    private async ValueTask DisposeScopeAsync()
    {
        if (activeScope is null)
        {
            return;
        }

        var scope = activeScope;
        activeScope = null;

        // Release the shared connection so subsequent commands fall back to independent connections.
        connectionManager.ClearTransaction(scope);
        await scope.DisposeAsync().ConfigureAwait(false);
    }
}
