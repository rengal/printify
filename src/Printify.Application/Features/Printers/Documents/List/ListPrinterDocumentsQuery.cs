using MediatR;
using Printify.Domain.Documents;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.List;

public sealed record ListPrinterDocumentsQuery(
    Guid PrinterId,
    RequestContext Context,
    Guid? BeforeId,
    int Limit)
    : IRequest<IReadOnlyList<Document>>;
