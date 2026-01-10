namespace Printify.Web.Contracts.Workspaces.Requests;

/// <summary>
/// Payload required to update a workspace.
/// </summary>
/// <param name="Name">Optional: New workspace display name.</param>
/// <param name="DocumentRetentionDays">Optional: Number of days to keep documents (1-365).</param>
public sealed record UpdateWorkspaceRequestDto(
    string? Name,
    int? DocumentRetentionDays);
