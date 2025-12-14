using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Status;

public sealed record SetPrinterTargetStateCommand(
    RequestContext Context,
    Guid PrinterId,
    PrinterTargetState TargetState) : IRequest<Printer>;
