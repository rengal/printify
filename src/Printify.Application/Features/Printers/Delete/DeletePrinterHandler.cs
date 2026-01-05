using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Delete;

public sealed class DeletePrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator)
    : IRequestHandler<DeletePrinterCommand, Unit>
{
    public async Task<Unit> Handle(DeletePrinterCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, ct)
            .ConfigureAwait(false);

        if (printer is null)
            throw new PrinterNotFoundException(request.PrinterId);

        var realtimeStatus = await printerRepository.GetRealtimeStatusAsync(printer.Id, ct).ConfigureAwait(false);
        // Default to Stopped to avoid keeping listeners alive for deleted printers with no realtime status.
        var targetState = realtimeStatus?.TargetState ?? PrinterTargetState.Stopped;
        await listenerOrchestrator.RemoveListenerAsync(printer, targetState, ct).ConfigureAwait(false);
        await printerRepository.DeleteAsync(printer, ct).ConfigureAwait(false);

        return Unit.Value;
    }
}
