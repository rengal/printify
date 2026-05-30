using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Mediation;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Delete;

public sealed class DeletePrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator)
    : IRequestHandler<DeletePrinterCommand, Unit>
{
    public async Task<Unit> Handle(IReceiveContext<DeletePrinterCommand> context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var printer = await PrinterAccess
            .GetWritablePrinterAsync(printerRepository, request.Context, request.PrinterId, ct)
            .ConfigureAwait(false);

        var operationalFlags = await printerRepository.GetOperationalFlagsAsync(printer.Id, ct).ConfigureAwait(false);
        // Default to Stopped to avoid keeping listeners alive for deleted printers without operational flags.
        var targetState = operationalFlags?.TargetState ?? PrinterTargetState.Stopped;
        await listenerOrchestrator.RemoveListenerAsync(printer, targetState, ct).ConfigureAwait(false);
        await printerRepository.DeleteAsync(printer, ct).ConfigureAwait(false);

        return Unit.Value;
    }
}

