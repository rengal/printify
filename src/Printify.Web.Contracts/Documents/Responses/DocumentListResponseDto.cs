namespace Printify.Web.Contracts.Documents.Responses;

using Printify.Web.Contracts.Common.Pagination;

/// <summary>
/// Wrapper containing a page of documents returned by listing endpoints.
/// </summary>
/// <param name="Result">Cursor-aware page metadata and the projected documents.</param>
public sealed record DocumentListResponseDto(PagedResult<DocumentDto> Result);
