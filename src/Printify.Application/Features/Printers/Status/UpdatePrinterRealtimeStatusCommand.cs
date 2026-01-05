using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Status;

public sealed record UpdatePrinterRealtimeStatusCommand(
    RequestContext Context,
    Guid PrinterId,
    string? TargetStatus,
    bool? IsCoverOpen,
    bool? IsPaperOut,
    bool? IsOffline,
    bool? HasError,
    bool? IsPaperNearEnd,
    string? Drawer1State,
    string? Drawer2State) : IRequest<PrinterRealtimeStatus>;
