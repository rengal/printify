namespace Printify.Web.Contracts.Common.Pagination;

/// <summary>
/// Represents a single page of results with cursor information for fetching subsequent pages.
/// </summary>
/// <typeparam name="T">Type of the returned items.</typeparam>
/// <param name="Items">Current page of items.</param>
/// <param name="HasMore">Indicates whether more items are available.</param>
/// <param name="NextBeforeId">Identifier cursor to request the next page; null when no more data.</param>
/// <param name="NextBeforeCreatedAt">Timestamp cursor paired with <paramref name="NextBeforeId"/>.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    bool HasMore,
    Guid? NextBeforeId,
    DateTimeOffset? NextBeforeCreatedAt);
