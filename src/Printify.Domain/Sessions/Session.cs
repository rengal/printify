namespace Printify.Domain.Sessions;

/// <summary>
/// Represents an anonymous browsing session that may later be linked to a user.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="CreatedAt">Timestamp when the session was created.</param>
/// <param name="LastActiveAt">Timestamp of the last observed activity.</param>
/// <param name="CreatedFromIp">Originating IP address.</param>
/// <param name="ClaimedUserId">Identifier of the user that claimed the session, if any.</param>
/// <param name="ExpiresAt">Timestamp when the session should expire.</param>
public sealed record Session(
    long Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActiveAt,
    string CreatedFromIp,
    long? ClaimedUserId,
    DateTimeOffset ExpiresAt);
