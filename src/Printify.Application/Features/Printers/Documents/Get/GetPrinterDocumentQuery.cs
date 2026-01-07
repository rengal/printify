using Mediator.Net.Contracts;
using Printify.Domain.Documents;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.Get;

public sealed record GetPrinterDocumentQuery(
    Guid PrinterId,
    Guid DocumentId,
    RequestContext Context) : IRequest;

