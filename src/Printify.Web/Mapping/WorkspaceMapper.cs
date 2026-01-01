using Printify.Application.Features.Workspaces.GetWorkspaceSummary;
using Printify.Domain.Workspaces;
using Printify.Web.Contracts.Workspaces.Responses;

namespace Printify.Web.Mapping;

internal static class WorkspaceMapper
{
    internal static WorkspaceResponseDto ToResponseDto(this Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return new WorkspaceResponseDto(workspace.Id, workspace.OwnerName, workspace.Token);
    }

    internal static WorkspaceDto ToDto(this Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return new WorkspaceDto(workspace.Id, workspace.OwnerName, workspace.CreatedAt);
    }

    internal static WorkspaceSummaryDto ToDto(this WorkspaceSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new WorkspaceSummaryDto(
            summary.TotalPrinters,
            summary.TotalDocuments,
            summary.DocumentsLast24h,
            summary.LastDocumentAt,
            summary.CreatedAt);
    }
}
