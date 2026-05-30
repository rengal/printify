namespace Printify.Web.Contracts.Workspaces.Responses;

public sealed record AdminWorkspaceStatisticsDto(
    int TotalWorkspaces,
    int ActiveWorkspacesLast24h,
    int ActiveWorkspacesLast7d,
    int TotalPrinters,
    long TotalDocuments,
    long TotalMedia,
    long TotalMediaBytes,
    long DocumentsLast24h,
    long DocumentsLast7d,
    long MediaLast24h,
    long MediaLast7d,
    DateTimeOffset? LastDocumentAt,
    IReadOnlyList<AdminWorkspaceStatisticsRowDto> Workspaces);

public sealed record AdminWorkspaceStatisticsRowDto(
    Guid WorkspaceId,
    string WorkspaceName,
    string Role,
    int PrinterCount,
    long DocumentCount,
    long MediaCount,
    long MediaBytes,
    long DocumentsLast24h,
    DateTimeOffset? LastDocumentAt,
    int DocumentRetentionDays);
