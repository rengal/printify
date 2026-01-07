using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Update;

public sealed class UpdatePrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator,
    IPrinterStatusStream statusStream,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<UpdatePrinterCommand, PrinterDetailsSnapshot>
{
    public async Task<PrinterDetailsSnapshot> Handle(IReceiveContext<UpdatePrinterCommand> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        if (request.Context.WorkspaceId is null)
        {
            throw new BadRequestException("Workspace identifier must be provided.");
        }

        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.WorkspaceId,
            cancellationToken).ConfigureAwait(false);

        if (printer is null)
        {
            throw new PrinterNotFoundException(request.PrinterId);
        }

        var settings = await printerRepository.GetSettingsAsync(printer.Id, cancellationToken)
            .ConfigureAwait(false);
        // Settings are persisted separately; missing settings indicate a data integrity issue.
        if (settings is null)
        {
            throw new InvalidOperationException($"Settings for printer {printer.Id} are missing.");
        }

        var updated = printer with
        {
            DisplayName = request.Printer.DisplayName
        };

        var updatedSettings = new PrinterSettings(
            request.Settings.Protocol,
            request.Settings.WidthInDots,
            request.Settings.HeightInDots,
            settings.ListenTcpPortNumber,
            request.Settings.EmulateBufferCapacity,
            request.Settings.BufferDrainRate,
            request.Settings.BufferMaxCapacity);

        await printerRepository.UpdateAsync(updated, updatedSettings, cancellationToken).ConfigureAwait(false);

        // Listener port is assigned by the server; no port updates allowed from client.

        // Restart the listener if the printer is already started
        var flags = await printerRepository.GetOperationalFlagsAsync(printer.Id, cancellationToken);
        // Default to Started to preserve the legacy target-state behavior for missing records.
        var targetState = flags?.TargetState ?? PrinterTargetState.Started;
        if (targetState == PrinterTargetState.Started)
        {
            await listenerOrchestrator.RemoveListenerAsync(printer, targetState, cancellationToken).ConfigureAwait(false);
            await listenerOrchestrator.AddListenerAsync(updated, updatedSettings, targetState, cancellationToken)
                .ConfigureAwait(false);
        }

        var settingsChanged = settings.Protocol != updatedSettings.Protocol
                              || settings.WidthInDots != updatedSettings.WidthInDots
                              || settings.HeightInDots != updatedSettings.HeightInDots
                              || settings.EmulateBufferCapacity != updatedSettings.EmulateBufferCapacity
                              || settings.BufferDrainRate != updatedSettings.BufferDrainRate
                              || settings.BufferMaxCapacity != updatedSettings.BufferMaxCapacity;
        var displayNameChanged = !string.Equals(printer.DisplayName, updated.DisplayName, StringComparison.Ordinal);
        if (settingsChanged || displayNameChanged)
        {
            var update = new PrinterStatusUpdate(
                updated.Id,
                DateTimeOffset.UtcNow,
                Settings: settingsChanged ? updatedSettings : null,
                Printer: displayNameChanged ? updated : null);
            statusStream.Publish(updated.OwnerWorkspaceId, update);
        }

        var runtimeStatus = runtimeStatusStore.Get(updated.Id);
        return new PrinterDetailsSnapshot(updated, updatedSettings, flags, runtimeStatus);
    }
}

