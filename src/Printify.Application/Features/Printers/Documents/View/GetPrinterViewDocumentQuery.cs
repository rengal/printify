using MediatR;
using Printify.Domain.Documents.View;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.View;

public sealed record GetPrinterViewDocumentQuery(
    Guid PrinterId,
    Guid DocumentId,
    RequestContext Context)
    : IRequest<ViewDocument?>;
