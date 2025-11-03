using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Create;

public sealed record CreatePrinterCommand(
    RequestContext Context,
    Guid PrinterId,
    string DisplayName,
    Protocol Protocol,
    int WidthInDots,
    int? HeightInDots,
    int? TcpListenPort,
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity)
    : IRequest<Printer>, ITransactionalRequest;
