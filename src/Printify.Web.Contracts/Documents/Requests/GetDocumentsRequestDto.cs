namespace Printify.Web.Contracts.Documents.Requests;

/// <summary>
/// Cursor-based query parameters for listing documents via the web API.
/// </summary>
/// <param name="Limit">Maximum number of documents to return.</param>
/// <param name="BeforeCreatedAt">Exclusive upper bound for the creation timestamp cursor.</param>
/// <param name="BeforeId">Exclusive upper bound for the identifier cursor when timestamps are equal.</param>
/// <param name="From">Inclusive lower bound for creation timestamp filtering.</param>
/// <param name="To">Inclusive upper bound for creation timestamp filtering.</param>
public sealed record GetDocumentsRequestDto(
    int Limit = 20,
    DateTimeOffset? BeforeCreatedAt = null,
    Guid? BeforeId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);
