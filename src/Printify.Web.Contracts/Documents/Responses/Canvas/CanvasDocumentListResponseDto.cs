using Printify.Web.Contracts.Common.Pagination;

namespace Printify.Web.Contracts.Documents.Responses.Canvas;

public sealed record CanvasDocumentListResponseDto(PagedResult<CanvasDocumentDto> Result);
