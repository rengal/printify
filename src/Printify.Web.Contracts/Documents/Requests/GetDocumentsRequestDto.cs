namespace Printify.Web.Contracts.Documents.Requests;

/// <summary>
/// Cursor-based query parameters for listing documents via the printer API.
/// </summary>
/// <param name="Limit">Maximum number of documents to return.</param>
/// <param name="BeforeId">Exclusive upper bound for identifier cursor when timestamps are equal.</param>
public sealed record GetDocumentsRequestDto(
    int Limit = 20,
    Guid? BeforeId = null);
