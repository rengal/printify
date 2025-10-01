namespace Printify.Contracts.Documents.Queries;

/// <summary>
/// Cursor-based query parameters for listing documents.
/// </summary>
/// <param name="Limit">Maximum number of documents to return.</param>
/// <param name="BeforeId">Exclusive upper bound for the identifier cursor.</param>
/// <param name="SourceIp">Optional filter by originating source IP.</param>
public sealed record ListQuery(
    int Limit,
    long? BeforeId,
    string? SourceIp);
