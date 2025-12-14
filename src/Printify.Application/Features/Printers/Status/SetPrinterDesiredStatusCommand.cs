using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Status;

public sealed record SetPrinterDesiredStatusCommand(
    RequestContext Context,
    Guid PrinterId,
    PrinterDesiredStatus DesiredStatus) : IRequest<Printer>;
