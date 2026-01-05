using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Pin;

public sealed class SetPrinterPinnedHandler(
    IPrinterRepository printerRepository,
    IPrinterStatusStream statusStream,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<SetPrinterPinnedCommand, PrinterDetailsSnapshot>
{
    public async Task<PrinterDetailsSnapshot> Handle(SetPrinterPinnedCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Context.WorkspaceId is null)
        {
            throw new BadRequestException("Workspace identifier must be provided.");
        }

        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        if (printer is null)
        {
            throw new PrinterNotFoundException(request.PrinterId);
        }

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
        return new PrinterDetailsSnapshot(updated, settings, flags, runtimeStatus);
    }
}
