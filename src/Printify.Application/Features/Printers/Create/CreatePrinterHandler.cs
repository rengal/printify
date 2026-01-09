using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Create;

public sealed class CreatePrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<CreatePrinterCommand, PrinterDetailsSnapshot>
{
    public async Task<PrinterDetailsSnapshot> Handle(
        IReceiveContext<CreatePrinterCommand> context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        var existing = await printerRepository
            .GetByIdAsync(request.Printer.Id, request.Context.WorkspaceId, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            var existingFlags = await printerRepository.GetOperationalFlagsAsync(existing.Id, ct)
                .ConfigureAwait(false);
            var existingSettings = await printerRepository.GetSettingsAsync(existing.Id, ct)
                .ConfigureAwait(false);
            // Settings are persisted separately; missing settings indicate a data integrity issue.
            if (existingSettings is null)
            {
                throw new InvalidOperationException($"Settings for printer {existing.Id} are missing.");
            }
            var existingRuntime = runtimeStatusStore.Get(existing.Id);
            return new PrinterDetailsSnapshot(existing, existingSettings, existingFlags, existingRuntime);
        }

        if (request.Context.WorkspaceId is null)
            throw new AuthenticationFailedException("Workspace is not set.");

        var listenTcpPortNumber = await printerRepository.GetFreeTcpPortNumber(ct).ConfigureAwait(false);

        var settings = new PrinterSettings(
            request.Settings.Protocol,
            request.Settings.WidthInDots,
            request.Settings.HeightInDots,
            listenTcpPortNumber,
            request.Settings.EmulateBufferCapacity,
            request.Settings.BufferDrainRate,
            request.Settings.BufferMaxCapacity);

        var printer = new Printer(
            request.Printer.Id,
            request.Context.WorkspaceId.Value,
            request.Printer.DisplayName,
            DateTimeOffset.UtcNow,
            request.Context.IpAddress,
            null,
            false,
            false,
            null,
            null);

        await printerRepository.AddAsync(printer, settings, ct).ConfigureAwait(false);

        // Seed operational flags so target state is stored outside the printer aggregate.
        var initialUpdate = new PrinterOperationalFlagsUpdate(
            printer.Id,
            DateTimeOffset.UtcNow,
            TargetState: PrinterTargetState.Started);
        await printerRepository.UpsertOperationalFlagsAsync(initialUpdate, ct).ConfigureAwait(false);

        var runtimeStatus = runtimeStatusStore.Get(printer.Id);
        var flags = new PrinterOperationalFlags(
            printer.Id,
            PrinterTargetState.Started,
            initialUpdate.UpdatedAt,
            false,
            false,
            false,
            false,
            false);

        // Start the printer listener asynchronously without blocking the response
        _ = Task.Run(async () =>
        {
            try
            {
                await listenerOrchestrator.AddListenerAsync(printer, settings, PrinterTargetState.Started, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                // Since this is fire-and-forget, we can't propagate the exception
                System.Diagnostics.Debug.WriteLine($"Failed to add printer listener: {ex}");
            }
        }, ct);


        return new PrinterDetailsSnapshot(printer, settings, flags, runtimeStatus);
    }
}

