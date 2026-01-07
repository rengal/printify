using Mediator.Net.Contracts;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Create;

public sealed record CreatePrinterCommand(
    RequestContext Context,
    CreatePrinterPayload Printer,
    CreatePrinterSettingsPayload Settings)
    : IRequest, ITransactionalRequest;

public sealed record CreatePrinterPayload(
    Guid Id,
    string DisplayName);

public sealed record CreatePrinterSettingsPayload(
    Protocol Protocol,
    int WidthInDots,
    int? HeightInDots,
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity);

