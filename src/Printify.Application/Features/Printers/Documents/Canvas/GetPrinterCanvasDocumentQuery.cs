using Mediator.Net.Contracts;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.Canvas;

public sealed record GetPrinterCanvasDocumentQuery(
    Guid PrinterId,
    Guid DocumentId,
    RequestContext Context)
    : IRequest;
