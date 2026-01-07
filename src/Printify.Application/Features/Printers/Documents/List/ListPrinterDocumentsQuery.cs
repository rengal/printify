using Mediator.Net.Contracts;
using Printify.Domain.Documents;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.List;

public sealed record ListPrinterDocumentsQuery(
    Guid PrinterId,
    RequestContext Context,
    Guid? BeforeId,
    int Limit) : IRequest;

public sealed record PrinterDocumentListResponse(IReadOnlyList<Document> Documents) : IResponse;
