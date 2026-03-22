using Mediator.Net.Contracts;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.Inject;

public sealed record InjectPrinterDocumentCommand(
    RequestContext Context,
    Guid PrinterId,
    ReadOnlyMemory<byte> Data) : IRequest;
