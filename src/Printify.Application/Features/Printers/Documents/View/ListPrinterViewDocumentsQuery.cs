using Mediator.Net.Contracts;
using Printify.Domain.Documents.View;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.View;

public sealed record ListPrinterViewDocumentsQuery(
    Guid PrinterId,
    RequestContext Context,
    Guid? BeforeId,
    int Limit) : IRequest;

public sealed record PrinterViewDocumentListResponse(IReadOnlyList<ViewDocument> Documents) : IResponse;
