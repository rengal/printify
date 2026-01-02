using MediatR;
using Printify.Domain.Documents.View;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.View;

public sealed record ListPrinterViewDocumentsQuery(
    Guid PrinterId,
    RequestContext Context,
    Guid? BeforeId,
    int Limit)
    : IRequest<IReadOnlyList<ViewDocument>>;
