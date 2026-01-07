namespace Printify.Application.Features.Workspaces.GetWorkspaceSummary;

using Mediator.Net.Contracts;

/// <summary>
/// Workspace summary with document statistics.
/// </summary>
/// <param name="TotalPrinters">Total number of printers in the workspace.</param>
/// <param name="TotalDocuments">Total number of documents across all printers.</param>
/// <param name="DocumentsLast24h">Number of documents received in the last 24 hours.</param>
/// <param name="LastDocumentAt">Timestamp of the most recent document, or null if no documents.</param>
/// <param name="CreatedAt">Timestamp when the workspace was created.</param>
public sealed record WorkspaceSummary(
    int TotalPrinters,
    long TotalDocuments,
    long DocumentsLast24h,
    DateTimeOffset? LastDocumentAt,
    DateTimeOffset CreatedAt) : IResponse;

