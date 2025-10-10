using Printify.Web.Contracts.Common.Pagination;

namespace Printify.Web.Contracts.Documents.Responses;

/// <summary>
/// Wrapper containing a page of documents returned by listing endpoints.
/// </summary>
/// <param name="Result">Cursor-aware page metadata and the projected documents.</param>
public sealed record DocumentListResponse(PagedResult<DocumentDto> Result);
