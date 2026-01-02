using Printify.Web.Contracts.Common.Pagination;

namespace Printify.Web.Contracts.Documents.Responses.View;

/// <summary>
/// Wrapper containing a page of view-oriented documents returned by listing endpoints.
/// </summary>
/// <param name="Result">Cursor-aware page metadata and the projected documents.</param>
public sealed record ViewDocumentListResponseDto(PagedResult<ViewDocumentDto> Result);
