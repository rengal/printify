namespace Printify.Web.Contracts.Workspaces.Responses;

/// <summary>
/// Lightweight workspace projection returned by the API.
/// </summary>
/// <param name="Id">Identifier of the workspace.</param>
/// <param name="WorkspaceName">Display name of the workspace.</param>
/// <param name="CreatedAt">Timestamp when the workspace was created.</param>
public sealed record WorkspaceDto(Guid Id, string WorkspaceName, DateTimeOffset CreatedAt);
