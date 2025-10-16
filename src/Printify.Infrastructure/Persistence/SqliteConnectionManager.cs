using System.Data;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Printify.Infrastructure.Config;

namespace Printify.Infrastructure.Persistence;

public sealed class SqliteConnectionManager
{
    private readonly RepositoryOptions options;
    private SqliteConnection? sharedConnection;
    private SqliteTransaction? activeTransaction;

    public SqliteConnectionManager(IOptions<RepositoryOptions> options)
    {
        this.options = options.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(this.options.ConnectionString))
        {
            throw new InvalidOperationException("RepositoryOptions.ConnectionString must be provided.");
        }
    }

    internal async ValueTask<SqliteConnectionLease> LeaseAsync(CancellationToken ct)
    {
        if (activeTransaction is not null && sharedConnection is not null)
        {
            // Share the transaction-bound connection so commands enlist automatically.
            return new SqliteConnectionLease(sharedConnection, activeTransaction, ownsConnection: false);
        }

        // Standalone operations get their own short-lived connection to avoid holding locks.
        var connection = new SqliteConnection(options.ConnectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return new SqliteConnectionLease(connection, null, ownsConnection: true);
    }

    internal async Task<SqliteTransactionScope> BeginTransactionAsync(CancellationToken ct)
    {
        if (activeTransaction is not null && sharedConnection is not null)
        {
            throw new InvalidOperationException("A transaction is already active for this scope.");
        }

        sharedConnection ??= new SqliteConnection(options.ConnectionString);

        if (sharedConnection.State != ConnectionState.Open)
        {
            // Lazily open the connection only when the first transactional request arrives.
            await sharedConnection.OpenAsync(ct).ConfigureAwait(false);
        }

        // SQLite returns a DbTransaction, but for precision we cast back to the provider type.
        activeTransaction = (SqliteTransaction)await sharedConnection.BeginTransactionAsync(ct).ConfigureAwait(false);
        return new SqliteTransactionScope(sharedConnection, activeTransaction);
    }

    internal void ClearTransaction(SqliteTransactionScope scope)
    {
        if (sharedConnection != scope.Connection || activeTransaction != scope.Transaction)
        {
            throw new InvalidOperationException("Attempted to clear a transaction scope that is not current.");
        }

        // Reset tracking so subsequent leases are not bound to a disposed connection.
        activeTransaction = null;
        sharedConnection = null;
    }
}
