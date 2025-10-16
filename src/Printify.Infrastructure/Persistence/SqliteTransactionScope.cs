using Microsoft.Data.Sqlite;

namespace Printify.Infrastructure.Persistence;

internal sealed class SqliteTransactionScope : IAsyncDisposable
{
    internal SqliteTransactionScope(SqliteConnection connection, SqliteTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    internal SqliteConnection Connection { get; }

    internal SqliteTransaction Transaction { get; }

    public async ValueTask DisposeAsync()
    {
        await Transaction.DisposeAsync().ConfigureAwait(false);
        await Connection.DisposeAsync().ConfigureAwait(false);
    }
}
