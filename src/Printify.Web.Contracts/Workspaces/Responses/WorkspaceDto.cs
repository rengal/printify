namespace Printify.Web.Contracts.Workspaces.Responses;

/// <summary>
/// Lightweight workspace projection returned by the API.
/// </summary>
/// <param name="Id">Identifier of the workspace.</param>
/// <param name="Name">Display name of the workspace.</param>
/// <param name="CreatedAt">Timestamp when the workspace was created.</param>
/// <param name="DocumentRetentionDays">Number of days to retain documents before automatic deletion.</param>
public sealed record WorkspaceDto(Guid Id, string Name, DateTimeOffset CreatedAt, int DocumentRetentionDays);
