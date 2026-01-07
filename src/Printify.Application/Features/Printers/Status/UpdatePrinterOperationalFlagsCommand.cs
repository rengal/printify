using Mediator.Net.Contracts;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Status;

public sealed record UpdatePrinterOperationalFlagsCommand(
    RequestContext Context,
    Guid PrinterId,
    bool? IsCoverOpen,
    bool? IsPaperOut,
    bool? IsOffline,
    bool? HasError,
    bool? IsPaperNearEnd,
    string? TargetState = null)
    : IRequest;

