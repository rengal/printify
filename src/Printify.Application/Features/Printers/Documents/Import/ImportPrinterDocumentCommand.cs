using Mediator.Net.Contracts;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Documents.Import;

public sealed record ImportPrinterDocumentCommand(
    RequestContext Context,
    Guid PrinterId,
    ReadOnlyMemory<byte> Data) : IRequest;
