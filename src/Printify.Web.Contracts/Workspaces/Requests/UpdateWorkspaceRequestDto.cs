namespace Printify.Web.Contracts.Workspaces.Requests;

/// <summary>
/// Payload required to update a workspace.
/// </summary>
/// <param name="Name">Optional: New workspace display name.</param>
/// <param name="DocumentRetentionDays">Optional: Number of days to keep documents (0 keeps forever).</param>
/// <param name="TcpWhitelistEnabled">Optional: Enable or disable the TCP IP whitelist.</param>
/// <param name="TcpWhitelistEntries">Optional: Newline-separated IP addresses or CIDR ranges allowed to connect via raw TCP.</param>
public sealed record UpdateWorkspaceRequestDto(
    string? Name,
    int? DocumentRetentionDays,
    bool? TcpWhitelistEnabled,
    string? TcpWhitelistEntries);
