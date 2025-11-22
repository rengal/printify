namespace Printify.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Printify.Web.Contracts.Documents.Requests;
using Printify.Web.Contracts.Documents.Responses;

/// <summary>
/// Exposes document listing and retrieval endpoints. Implementation pending.
/// </summary>
[ApiController]
[Route("api/printers/{printerId:guid}/documents")]
public sealed class DocumentsController : ControllerBase
{
    [HttpGet]
    public ActionResult<DocumentListResponseDto> ListDocuments(
        Guid printerId,
        [FromQuery] GetDocumentsRequestDto request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpGet("{documentId:guid}")]
    public ActionResult<DocumentDto> GetDocument(Guid printerId, Guid documentId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPost("last-viewed")]
    public IActionResult SetLastViewedDocument(Guid printerId, [FromBody] SetLastViewedDocumentRequestDto request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpGet("stats")]
    public ActionResult<DocumentStatsResponseDto> GetDocumentStats(Guid printerId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
