using System.Globalization;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Printify.Application.Interfaces;
using Printify.Domain.AnonymousSessions;

namespace Printify.Infrastructure.Persistence;

public sealed class AnonymousSessionRepository(
    SqliteConnectionManager connectionManager,
    ILogger<AnonymousSessionRepository> logger)
    : IAnonymousSessionRepository
{
    private const string TableName = "anonymous_sessions";
    private const string ColumnList = "id, created_at, last_active_at, created_from_ip, linked_user_id";

    private readonly SemaphoreSlim schemaSemaphore = new(1, 1);
    private int schemaInitialized;

    public async ValueTask<AnonymousSession?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        // Ensure the backing table exists before attempting to read.
        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        // Lease a connection so we participate in an ambient SQLite transaction when present.
        await using var lease = await connectionManager.LeaseAsync(ct).ConfigureAwait(false);
        await using var command = lease.Connection.CreateCommand();
        command.CommandText = $"""
            SELECT {ColumnList}
            FROM {TableName}
            WHERE id = $id
            LIMIT 1;
            """;
        command.Transaction = lease.Transaction;
        command.Parameters.AddWithValue("$id", id.ToString("D", CultureInfo.InvariantCulture));

        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return ReadSession(reader);
    }

    public async ValueTask<AnonymousSession> AddAsync(AnonymousSession session, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Make sure schema creation happened exactly once across threads.
        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        // Reuse the transactional connection if the caller started a unit of work.
        await using var lease = await connectionManager.LeaseAsync(ct).ConfigureAwait(false);
        await using var command = lease.Connection.CreateCommand();
        command.Transaction = lease.Transaction;
        command.CommandText = $"""
            INSERT INTO {TableName} (id, created_at, last_active_at, created_from_ip, linked_user_id)
            VALUES ($id, $created_at, $last_active_at, $created_from_ip, $linked_user_id);
            """;
        BindSessionParameters(command, session);

        var affectedRows = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        if (affectedRows != 1)
        {
            throw new InvalidOperationException("Failed to insert anonymous session.");
        }

        logger.LogDebug("Anonymous session {SessionId} created.", session.Id);
        return session;
    }

    public async Task UpdateAsync(AnonymousSession session, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Schema guard is a no-op after the table is materialised.
        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        // Align updates with any active transaction to keep state consistent.
        await using var lease = await connectionManager.LeaseAsync(ct).ConfigureAwait(false);
        await using var command = lease.Connection.CreateCommand();
        command.Transaction = lease.Transaction;
        command.CommandText = $"""
            UPDATE {TableName}
            SET created_at = $created_at,
                last_active_at = $last_active_at,
                created_from_ip = $created_from_ip,
                linked_user_id = $linked_user_id
            WHERE id = $id;
            """;
        BindSessionParameters(command, session);

        var affectedRows = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Session {session.Id} update failed or session does not exist.");
        }
    }

    public async Task TouchAsync(Guid id, DateTimeOffset lastActive, CancellationToken ct)
    {
        // Guard to avoid touching a table that might not exist on first run.
        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        // This update participates in an outer transaction when present.
        await using var lease = await connectionManager.LeaseAsync(ct).ConfigureAwait(false);
        await using var command = lease.Connection.CreateCommand();
        command.Transaction = lease.Transaction;
        command.CommandText = $"""
            UPDATE {TableName}
            SET last_active_at = $last_active_at
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$last_active_at", lastActive.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$id", id.ToString("D", CultureInfo.InvariantCulture));

        var affectedRows = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Unable to update last activity timestamp for anonymous session {id}.");
        }
    }

    public async Task AttachUserAsync(Guid id, Guid userId, CancellationToken ct)
    {
        // Schema must exist because this command links two domain concepts.
        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        // Rely on the caller's transaction to keep the link atomic with other changes.
        await using var lease = await connectionManager.LeaseAsync(ct).ConfigureAwait(false);
        await using var command = lease.Connection.CreateCommand();
        command.Transaction = lease.Transaction;
        command.CommandText = $"""
            UPDATE {TableName}
            SET linked_user_id = $linked_user_id
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$linked_user_id", userId.ToString("D", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$id", id.ToString("D", CultureInfo.InvariantCulture));

        var affectedRows = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Failed to attach user {userId} to anonymous session {id}.");
        }
    }

    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (schemaInitialized == 1)
        {
            return;
        }

        // Serialize schema creation so multiple threads do not race on the CREATE TABLE statement.
        await schemaSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (schemaInitialized == 1)
            {
                return;
            }

            // Acquire a connection (transactional if available) and materialise the table idempotently.
            await using var lease = await connectionManager.LeaseAsync(ct).ConfigureAwait(false);
            await using var command = lease.Connection.CreateCommand();
            command.Transaction = lease.Transaction;
            command.CommandText = $"""
                CREATE TABLE IF NOT EXISTS {TableName}
                (
                    id TEXT PRIMARY KEY,
                    created_at TEXT NOT NULL,
                    last_active_at TEXT NOT NULL,
                    created_from_ip TEXT NOT NULL,
                    linked_user_id TEXT NULL
                );
                """;

            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            schemaInitialized = 1;
        }
        finally
        {
            schemaSemaphore.Release();
        }
    }

    private static void BindSessionParameters(SqliteCommand command, AnonymousSession session)
    {
        // Persist timestamps in round-trip format so offsets survive round-trips.
        command.Parameters.AddWithValue("$id", session.Id.ToString("D", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$created_at", session.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$last_active_at", session.LastActiveAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$created_from_ip", session.CreatedFromIp);
        if (session.LinkedUserId.HasValue)
        {
            command.Parameters.AddWithValue("$linked_user_id", session.LinkedUserId.Value.ToString("D", CultureInfo.InvariantCulture));
        }
        else
        {
            command.Parameters.AddWithValue("$linked_user_id", DBNull.Value);
        }
    }

    private static AnonymousSession ReadSession(SqliteDataReader reader)
    {
        var id = Guid.Parse(reader.GetString(0));
        var createdAt = DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var lastActiveAt = DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var createdFromIp = reader.GetString(3);
        Guid? linkedUserId = null;

        if (!reader.IsDBNull(4))
        {
            var value = reader.GetString(4);
            if (!string.IsNullOrWhiteSpace(value))
            {
                linkedUserId = Guid.Parse(value);
            }
        }

        return new AnonymousSession(id, createdAt, lastActiveAt, createdFromIp, linkedUserId);
    }
}
