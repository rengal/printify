using Mediator.Net.Contracts;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.Clear;

public sealed record ClearPrinterDocumentsCommand(
    RequestContext Context,
    Guid PrinterId) : IRequest;

