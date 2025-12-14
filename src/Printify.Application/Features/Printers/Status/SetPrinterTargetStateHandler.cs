using System.Net.Sockets;
using MediatR;
using Microsoft.Extensions.Logging;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Status;

public sealed class SetPrinterTargetStateHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator,
    ILogger<SetPrinterTargetStateHandler> logger)
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
            logger.LogWarning(
                "Printer {PrinterId} not found for workspace {WorkspaceId} when setting target state to {TargetState}",
                request.PrinterId,
                request.Context.WorkspaceId,
                request.TargetState);
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
                logger.LogInformation(
                    "Starting listener for printer {PrinterId} in workspace {WorkspaceId}",
                    request.PrinterId,
                    request.Context.WorkspaceId);
                await listenerOrchestrator.AddListenerAsync(updated, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                logger.LogWarning(
                    "Socket error while starting listener for printer {PrinterId} in workspace {WorkspaceId}",
                    request.PrinterId,
                    request.Context.WorkspaceId);
                throw;
            }
        }
        else
        {
            logger.LogInformation(
                "Stopping listener for printer {PrinterId} in workspace {WorkspaceId}",
                request.PrinterId,
                request.Context.WorkspaceId);
            await listenerOrchestrator.RemoveListenerAsync(updated, cancellationToken).ConfigureAwait(false);
        }

        var refreshed = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        return refreshed ?? updated;
    }
}
