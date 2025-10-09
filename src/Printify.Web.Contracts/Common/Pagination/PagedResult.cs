namespace Printify.Domain.Documents.Queries;

/// <summary>
/// Represents a single page of results with cursor information for fetching subsequent pages.
/// </summary>
/// <typeparam name="T">Type of the returned items.</typeparam>
/// <param name="Items">Current page of items.</param>
/// <param name="HasMore">Indicates whether more items are available.</param>
/// <param name="NextBeforeId">Cursor to request the next page; null when no more data.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    bool HasMore,
    long? NextBeforeId);
