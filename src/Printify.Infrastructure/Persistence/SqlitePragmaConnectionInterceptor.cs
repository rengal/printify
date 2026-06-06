using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Printify.Infrastructure.Persistence;

/// <summary>
/// Applies SQLite PRAGMAs on every opened connection so concurrency settings are owned by the app
/// rather than left to whatever was persisted in the database file.
/// </summary>
/// <remarks>
/// busy_timeout makes a connection wait for a lock to clear instead of failing immediately with
/// SQLITE_BUSY. Without it, a long-running retention write blocks printers/SSE readers and the app
/// appears to hang. WAL keeps readers and a single writer concurrent.
/// </remarks>
public sealed class SqlitePragmaConnectionInterceptor : DbConnectionInterceptor
{
    private const int BusyTimeoutMs = 30_000;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyPragmas(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ApplyPragmas(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);
    }

    private static void ApplyPragmas(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA busy_timeout={BusyTimeoutMs}; PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();
    }
}
