using Microsoft.Data.Sqlite;

namespace Printify.Infrastructure.Persistence;

internal sealed class SqliteConnectionLease : IAsyncDisposable
{
    private readonly bool ownsConnection;

    internal SqliteConnectionLease(SqliteConnection connection, SqliteTransaction? transaction, bool ownsConnection)
    {
        Connection = connection;
        Transaction = transaction;
        this.ownsConnection = ownsConnection;
    }

    internal SqliteConnection Connection { get; }

    internal SqliteTransaction? Transaction { get; }

    public async ValueTask DisposeAsync()
    {
        if (!ownsConnection)
        {
            return;
        }

        await Connection.DisposeAsync().ConfigureAwait(false);
    }
}
