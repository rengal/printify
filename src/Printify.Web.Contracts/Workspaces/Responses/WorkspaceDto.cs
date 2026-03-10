namespace Printify.Web.Contracts.Workspaces.Responses;

/// <summary>
/// Lightweight workspace projection returned by the API.
/// </summary>
/// <param name="Id">Identifier of the workspace.</param>
/// <param name="Name">Display name of the workspace.</param>
/// <param name="CreatedAt">Timestamp when the workspace was created.</param>
/// <param name="DocumentRetentionDays">Number of days to retain documents before automatic deletion.</param>
/// <param name="TcpWhitelistEnabled">Whether the TCP IP whitelist is active.</param>
/// <param name="TcpWhitelistEntries">Newline-separated list of allowed IPs / CIDR ranges.</param>
public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    int DocumentRetentionDays,
    bool TcpWhitelistEnabled,
    string TcpWhitelistEntries);
