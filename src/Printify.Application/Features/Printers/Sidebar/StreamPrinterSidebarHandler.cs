using System.Runtime.CompilerServices;
using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Microsoft.Extensions.Logging;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed class StreamPrinterSidebarHandler(
    IPrinterRepository printerRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore,
    IPrinterStatusStream statusStream,
    ILogger<StreamPrinterSidebarHandler> logger)
    : IRequestHandler<StreamPrinterSidebarQuery, PrinterSidebarStreamResult>
{
    public async Task<PrinterSidebarStreamResult> Handle(
        IReceiveContext<StreamPrinterSidebarQuery> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        if (request.Context.WorkspaceId is null)
        {
            throw new BadRequestException("Workspace identifier must be provided.");
        }

        var updates = ReadUpdatesAsync(
            request.Context.WorkspaceId.Value,
            cancellationToken);

        return new PrinterSidebarStreamResult("sidebar", updates);
    }

    private async IAsyncEnumerable<PrinterSidebarSnapshot> ReadUpdatesAsync(
        Guid workspaceId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var update in statusStream.Subscribe(workspaceId, ct))
        {
            logger.LogInformation(
                "Sidebar stream received update for printer {PrinterId} in workspace {WorkspaceId}",
                update.PrinterId,
                workspaceId);
            var hasStateChange = update.RuntimeUpdate is not null;
            var hasPrinterChange = update.Printer is not null;
            if (!hasStateChange && !hasPrinterChange)
            {
                logger.LogInformation(
                    "Sidebar stream skipped update for printer {PrinterId} (no state/printer changes)",
                    update.PrinterId);
                continue;
            }

            var printer = update.Printer ?? await printerRepository
                .GetByIdAsync(update.PrinterId, workspaceId, ct)
                .ConfigureAwait(false);
            if (printer is null)
            {
                logger.LogWarning(
                    "Sidebar stream skipped update for printer {PrinterId} (printer not found)",
                    update.PrinterId);
                continue;
            }

            var runtimeStatus = runtimeStatusStore.Get(printer.Id);
            logger.LogInformation(
                "Sidebar stream emitting snapshot for printer {PrinterId} with state {State}",
                printer.Id,
                runtimeStatus?.State);
            yield return new PrinterSidebarSnapshot(printer, runtimeStatus);
        }
    }
}

