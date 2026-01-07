using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.List;

public sealed class ListPrintersHandler(
    IPrinterRepository printerRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<ListPrintersQuery, PrinterListResponse>
{
    public async Task<PrinterListResponse> Handle(
        IReceiveContext<ListPrintersQuery> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var requestContext = request.Context;

        if (requestContext.WorkspaceId is null)
            throw new BadRequestException("Workspace identifier must be provided.");

        var printers = await printerRepository
            .ListOwnedAsync(requestContext.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        if (printers.Count == 0)
        {
            return new PrinterListResponse([]);
        }

        var flags = await printerRepository
            .ListOperationalFlagsAsync(requestContext.WorkspaceId.Value, cancellationToken)
            .ConfigureAwait(false);
        var settings = await printerRepository
            .ListSettingsAsync(requestContext.WorkspaceId.Value, cancellationToken)
            .ConfigureAwait(false);

        var snapshots = printers
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

        return new PrinterListResponse(snapshots);
    }
}

