using System.Net.Sockets;
using MediatR;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Status;

public sealed class SetPrinterTargetStateHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator)
    : IRequestHandler<SetPrinterTargetStateCommand, Printer>
{
    public async Task<Printer> Handle(SetPrinterTargetStateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        if (printer is null)
        {
            throw new InvalidOperationException("Printer not found.");
        }

        await printerRepository.SetTargetStateAsync(
            request.PrinterId,
            request.TargetState,
            cancellationToken).ConfigureAwait(false);

        var updated = printer with { TargetState = request.TargetState };

        if (request.TargetState == PrinterTargetState.Started)
        {
            try
            {
                await listenerOrchestrator.AddListenerAsync(updated, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                throw;
            }
        }
        else
        {
            await listenerOrchestrator.RemoveListenerAsync(updated, cancellationToken).ConfigureAwait(false);
        }

        var refreshed = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        return refreshed ?? updated;
    }
}
