using System;
using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Update;

public sealed record UpdatePrinterCommand(
    RequestContext Context,
    Guid PrinterId,
    string DisplayName,
    Protocol Protocol,
    int WidthInDots,
    int? HeightInDots,
    int? TcpListenPort,
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity) : IRequest<Printer>;
