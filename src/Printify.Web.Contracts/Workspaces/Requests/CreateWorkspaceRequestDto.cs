namespace Printify.Web.Contracts.Workspaces.Requests;

/// <summary>
/// Payload required to create a new workspace.
/// </summary>
/// <param name="Id">Client-generated identifier.</param>
/// <param name="WorkspaceName">Workspace name.</param>
public sealed record CreateWorkspaceRequestDto(Guid Id, string WorkspaceName);
