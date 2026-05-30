using Printify.Application.Features.Workspaces.GetAdminWorkspaceStatistics;
using Printify.Web.Contracts.Workspaces.Responses;

namespace Printify.Web.Mapping;

internal static class AdminWorkspaceStatisticsMapper
{
    internal static AdminWorkspaceStatisticsDto ToDto(this AdminWorkspaceStatistics statistics)
    {
        return new AdminWorkspaceStatisticsDto(
            statistics.TotalWorkspaces,
            statistics.ActiveWorkspacesLast24h,
            statistics.ActiveWorkspacesLast7d,
            statistics.TotalPrinters,
            statistics.TotalDocuments,
            statistics.TotalMedia,
            statistics.TotalMediaBytes,
            statistics.DocumentsLast24h,
            statistics.DocumentsLast7d,
            statistics.MediaLast24h,
            statistics.MediaLast7d,
            statistics.LastDocumentAt,
            statistics.Workspaces.Select(row => row.ToDto()).ToArray());
    }

    private static AdminWorkspaceStatisticsRowDto ToDto(this AdminWorkspaceStatisticsRow row)
    {
        return new AdminWorkspaceStatisticsRowDto(
            row.WorkspaceId,
            row.WorkspaceName,
            row.Role,
            row.PrinterCount,
            row.DocumentCount,
            row.MediaCount,
            row.MediaBytes,
            row.DocumentsLast24h,
            row.LastDocumentAt,
            row.DocumentRetentionDays);
    }
}
