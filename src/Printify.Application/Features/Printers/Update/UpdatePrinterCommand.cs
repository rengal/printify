using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Update;

public sealed record UpdatePrinterCommand(
    RequestContext Context,
    Guid PrinterId,
    UpdatePrinterPayload Printer,
    UpdatePrinterSettingsPayload Settings) : IRequest<PrinterDetailsSnapshot>;

public sealed record UpdatePrinterPayload(
    string DisplayName);

public sealed record UpdatePrinterSettingsPayload(
    Protocol Protocol,
    int WidthInDots,
    int? HeightInDots,
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity);
