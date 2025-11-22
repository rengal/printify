namespace Printify.Web.Contracts.Documents.Responses;

/// <summary>
/// Aggregated document counts for a printer.
/// </summary>
/// <param name="NewDocuments">Number of documents created since the last viewed marker.</param>
/// <param name="TotalDocuments">Total number of documents stored for the printer.</param>
public sealed record DocumentStatsResponseDto(int NewDocuments, long TotalDocuments);
