using MediatR;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Status;

public sealed class SetPrinterDesiredStatusHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator)
    : IRequestHandler<SetPrinterDesiredStatusCommand, Printer>
{
    public async Task<Printer> Handle(SetPrinterDesiredStatusCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        if (printer is null)
        {
            throw new InvalidOperationException("Printer not found.");
        }

        await printerRepository.SetDesiredStatusAsync(
            request.PrinterId,
            request.DesiredStatus,
            cancellationToken).ConfigureAwait(false);

        var updated = printer with { DesiredStatus = request.DesiredStatus };

        if (request.DesiredStatus == PrinterDesiredStatus.Started)
        {
            await listenerOrchestrator.AddListenerAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await listenerOrchestrator.RemoveListenerAsync(updated, cancellationToken).ConfigureAwait(false);
            await printerRepository.SetRuntimeStatusAsync(
                updated.Id,
                PrinterRuntimeStatus.Stopped,
                DateTimeOffset.UtcNow,
                null,
                cancellationToken).ConfigureAwait(false);
        }

        var refreshed = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        return refreshed ?? updated;
    }
}
