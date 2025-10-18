namespace Printify.Domain.AnonymousSessions;

/// <summary>
/// Represents an anonymous browsing session that may later be linked to a user account.
/// </summary>
/// <param name="Id">Unique identifier of the session.</param>
/// <param name="CreatedAt">Timestamp when the session was first created.</param>
/// <param name="LastActiveAt">Timestamp of the most recent activity within this session.</param>
/// <param name="CreatedFromIp">Originating client IP address.</param>
/// <param name="LinkedUserId">Identifier of the user, if this session has been linked after login; otherwise <c>null</c>.</param>
/// <param name="IsDeleted">Soft-delete marker for the session.</param>
public sealed record AnonymousSession(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActiveAt,
    string CreatedFromIp,
    Guid? LinkedUserId,
    bool IsDeleted)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted)
{
    public static AnonymousSession Create(string createdFromIp)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, createdFromIp, null, false);

    public AnonymousSession Touch()
        => this with { LastActiveAt = DateTimeOffset.UtcNow };

    public AnonymousSession LinkUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (LinkedUserId == userId)
            return this;

        if (LinkedUserId is not null)
            throw new InvalidOperationException("Session is already linked to a user.");

        return this with { LinkedUserId = userId };
    }
}
