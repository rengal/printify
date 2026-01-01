namespace Printify.Web.Contracts.Workspaces.Responses;

/// <summary>
/// Lightweight workspace projection returned by the API.
/// </summary>
/// <param name="Id">Identifier of the workspace.</param>
/// <param name="OwnerName">Display name of the workspace owner.</param>
/// <param name="CreatedAt">Timestamp when the workspace was created.</param>
public sealed record WorkspaceDto(Guid Id, string OwnerName, DateTimeOffset CreatedAt);
