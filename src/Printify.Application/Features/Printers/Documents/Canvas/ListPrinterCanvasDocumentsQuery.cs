using Mediator.Net.Contracts;
using Printify.Domain.Documents;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.Canvas;

public sealed record ListPrinterCanvasDocumentsQuery(
    Guid PrinterId,
    RequestContext Context,
    Guid? BeforeId,
    int Limit)
    : IRequest;

public sealed record PrinterCanvasDocumentListResponse(IReadOnlyList<RenderedDocument> Documents) : IResponse;
