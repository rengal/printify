using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.List;

public sealed class ListPrintersHandler(
    IPrinterRepository printerRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<ListPrintersQuery, IReadOnlyList<PrinterDetailsSnapshot>>
{
    public async Task<IReadOnlyList<PrinterDetailsSnapshot>> Handle(
        ListPrintersQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = request.Context;

        if (context.WorkspaceId is null)
            throw new BadRequestException("Workspace identifier must be provided.");

        var printers = await printerRepository
            .ListOwnedAsync(context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        if (printers.Count == 0)
        {
            return [];
        }

        var flags = await printerRepository
            .ListOperationalFlagsAsync(context.WorkspaceId.Value, cancellationToken)
            .ConfigureAwait(false);
        var settings = await printerRepository
            .ListSettingsAsync(context.WorkspaceId.Value, cancellationToken)
            .ConfigureAwait(false);

        return printers
            .Select(printer =>
            {
                flags.TryGetValue(printer.Id, out var operationalFlags);
                // Settings are persisted separately; missing settings indicate a data integrity issue.
                if (!settings.TryGetValue(printer.Id, out var printerSettings))
                {
                    throw new InvalidOperationException($"Settings for printer {printer.Id} are missing.");
                }
                var runtimeStatus = runtimeStatusStore.Get(printer.Id);
                return new PrinterDetailsSnapshot(printer, printerSettings, operationalFlags, runtimeStatus);
            })
            .ToList();
    }
}
