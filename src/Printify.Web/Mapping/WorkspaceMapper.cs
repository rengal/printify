using Printify.Application.Features.Workspaces.GetWorkspaceSummary;
using Printify.Application.Services;
using Printify.Domain.Workspaces;
using Printify.Web.Contracts.Workspaces.Responses;

namespace Printify.Web.Mapping;

internal static class WorkspaceMapper
{
    internal static WorkspaceResponseDto ToResponseDto(this Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return new WorkspaceResponseDto(workspace.Id, workspace.Name, workspace.Token);
    }

    internal static WorkspaceDto ToDto(this Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return new WorkspaceDto(workspace.Id, workspace.Name, workspace.CreatedAt);
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

    internal static GreetingResponseDto ToDto(this GreetingResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new GreetingResponseDto(
            response.Morning,
            response.Afternoon,
            response.Evening,
            response.General);
    }
}
