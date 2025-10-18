namespace Printify.Domain.Users;

/// <summary>
/// Person interacting with the system. Acts as a logical owner for printers and documents.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="DisplayName">Friendly name surfaced to UI.</param>
/// <param name="CreatedAt">Creation timestamp in UTC.</param>
/// <param name="CreatedFromIp">IP address captured when the user was registered.</param>
/// <param name="IsDeleted">Soft-delete marker for the user.</param>
public sealed record User(
    Guid Id,
    string DisplayName,
    DateTimeOffset CreatedAt,
    string CreatedFromIp,
    bool IsDeleted)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted);
