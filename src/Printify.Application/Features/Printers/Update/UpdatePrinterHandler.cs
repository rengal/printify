using MediatR;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Update;

public sealed class UpdatePrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator)
    : IRequestHandler<UpdatePrinterCommand, Printer>
{
    public async Task<Printer> Handle(UpdatePrinterCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.WorkspaceId,
            cancellationToken).ConfigureAwait(false);

        if (printer is null)
        {
            throw new InvalidOperationException("Printer not found.");
        }

        var updated = printer with
        {
            DisplayName = request.DisplayName,
            Protocol = request.Protocol,
            WidthInDots = request.WidthInDots,
            HeightInDots = request.HeightInDots,
            EmulateBufferCapacity = request.EmulateBufferCapacity,
            BufferDrainRate = request.BufferDrainRate,
            BufferMaxCapacity = request.BufferMaxCapacity
        };

        await printerRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);

        // Listener port is assigned by the server; no port updates allowed from client.

        // Restart the listener if the printer is already started
        if (printer.TargetState == PrinterTargetState.Started)
        {
            await listenerOrchestrator.RemoveListenerAsync(printer, cancellationToken).ConfigureAwait(false);
            await listenerOrchestrator.AddListenerAsync(updated, cancellationToken).ConfigureAwait(false);
        }

        return updated;
    }
}
