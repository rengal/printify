using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Status;

public sealed record UpdatePrinterDrawerStateCommand(
    RequestContext Context,
    Guid PrinterId,
    string? Drawer1State,
    string? Drawer2State)
    : IRequest<PrinterRuntimeStatus>;
