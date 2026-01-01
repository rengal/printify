namespace Printify.Web.Contracts.Workspaces.Responses;

/// <summary>
/// Workspace summary with document statistics for contextual UI.
/// </summary>
/// <param name="TotalPrinters">Total number of printers in the workspace.</param>
/// <param name="TotalDocuments">Total number of documents across all printers.</param>
/// <param name="DocumentsLast24h">Number of documents received in the last 24 hours.</param>
/// <param name="LastDocumentAt">Timestamp of the most recent document, or null if no documents.</param>
/// <param name="CreatedAt">Timestamp when the workspace was created.</param>
public sealed record WorkspaceSummaryDto(
    int TotalPrinters,
    long TotalDocuments,
    long DocumentsLast24h,
    DateTimeOffset? LastDocumentAt,
    DateTimeOffset CreatedAt);
