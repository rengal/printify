namespace Printify.Domain.Workspaces;

using Mediator.Net.Contracts;

/// <summary>
/// Person interacting with the system. Acts as a logical owner for printers and documents.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="OwnerName">Friendly name surfaced to UI.</param>
/// <param name="Token">Unique workspace token identifier (e.g., brave-tiger-1234) used for authentication.</param>
/// <param name="CreatedAt">Creation timestamp in UTC.</param>
/// <param name="CreatedFromIp">IP address captured when the workspace was registered.</param>
/// <param name="IsDeleted">Soft-delete marker for the workspace.</param>
public sealed record Workspace(
    Guid Id,
    string OwnerName,
    string Token,
    DateTimeOffset CreatedAt,
    string CreatedFromIp,
    bool IsDeleted)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted), IResponse;

