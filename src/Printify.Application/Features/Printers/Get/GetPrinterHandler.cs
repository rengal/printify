using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Get;

public sealed class GetPrinterHandler(
    IPrinterRepository printerRepository,
    IWorkspaceRepository workspaceRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<GetPrinterQuery, PrinterDetailsSnapshot?>
{
    public async Task<PrinterDetailsSnapshot?> Handle(IReceiveContext<GetPrinterQuery> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var printer = await PrinterAccess.GetReadablePrinterAsync(
            printerRepository,
            workspaceRepository,
            request.Context,
            request.PrinterId,
            cancellationToken);

        if (printer is null)
        {
            return null;
        }

        var flags = await printerRepository.GetOperationalFlagsAsync(printer.Id, cancellationToken)
            .ConfigureAwait(false);
        var settings = await printerRepository.GetSettingsAsync(printer.Id, cancellationToken)
            .ConfigureAwait(false);
        // Settings are persisted separately; missing settings indicate a data integrity issue.
        if (settings is null)
        {
            throw new InvalidOperationException($"Settings for printer {printer.Id} are missing.");
        }
        var runtimeStatus = runtimeStatusStore.Get(printer.Id);
        var ownerWorkspace = await workspaceRepository
            .GetByIdAsync(printer.OwnerWorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        return new PrinterDetailsSnapshot(printer, settings, flags, runtimeStatus, ownerWorkspace?.Name);
    }
}

