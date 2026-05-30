using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Pin;

public sealed class SetPrinterPinnedHandler(
    IPrinterRepository printerRepository,
    IWorkspaceRepository workspaceRepository,
    IPrinterStatusStream statusStream,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<SetPrinterPinnedCommand, PrinterDetailsSnapshot>
{
    public async Task<PrinterDetailsSnapshot> Handle(IReceiveContext<SetPrinterPinnedCommand> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var printer = await PrinterAccess
            .GetWritablePrinterAsync(printerRepository, request.Context, request.PrinterId, cancellationToken)
            .ConfigureAwait(false);

        await printerRepository
            .SetPinnedAsync(request.PrinterId, request.IsPinned, cancellationToken)
            .ConfigureAwait(false);

        var updated = printer with { IsPinned = request.IsPinned };
        var update = new PrinterStatusUpdate(
            updated.Id,
            DateTimeOffset.UtcNow,
            Printer: updated);
        statusStream.Publish(updated.OwnerWorkspaceId, update);
        var flags = await printerRepository.GetOperationalFlagsAsync(updated.Id, cancellationToken)
            .ConfigureAwait(false);
        var settings = await printerRepository.GetSettingsAsync(updated.Id, cancellationToken)
            .ConfigureAwait(false);
        // Settings are persisted separately; missing settings indicate a data integrity issue.
        if (settings is null)
        {
            throw new InvalidOperationException($"Settings for printer {updated.Id} are missing.");
        }
        var runtimeStatus = runtimeStatusStore.Get(updated.Id);
        var ownerWorkspace = await workspaceRepository.GetByIdAsync(updated.OwnerWorkspaceId, cancellationToken)
            .ConfigureAwait(false);
        return new PrinterDetailsSnapshot(updated, settings, flags, runtimeStatus, ownerWorkspace?.Name);
    }
}

