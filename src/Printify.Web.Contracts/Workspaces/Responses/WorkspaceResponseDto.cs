namespace Printify.Web.Contracts.Workspaces.Responses;

/// <summary>
/// Payload required to create a new workspace.
/// </summary>
/// <param name="Id">Client-generated identifier.</param>
/// <param name="OwnerName">Workspace owner.</param>
/// <param name="Token">Workspace access token.</param>
public sealed record WorkspaceResponseDto(Guid Id, string OwnerName, string Token);
