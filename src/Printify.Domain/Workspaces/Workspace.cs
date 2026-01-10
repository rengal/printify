namespace Printify.Domain.Workspaces;

using Mediator.Net.Contracts;

/// <summary>
/// Workspace representing a user's isolated environment for printers and documents.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="Name">Workspace display name.</param>
/// <param name="Token">Unique workspace token identifier (e.g., brave-tiger-1234) used for authentication.</param>
/// <param name="CreatedAt">Creation timestamp in UTC.</param>
/// <param name="CreatedFromIp">IP address captured when the workspace was registered.</param>
/// <param name="DocumentRetentionDays">Number of days to keep documents before automatic cleanup (1-365).</param>
/// <param name="IsDeleted">Soft-delete marker for the workspace.</param>
public sealed record Workspace(
    Guid Id,
    string Name,
    string Token,
    DateTimeOffset CreatedAt,
    string CreatedFromIp,
    int DocumentRetentionDays,
    bool IsDeleted)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted), IResponse;

