using System.Runtime.CompilerServices;
using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed class StreamPrinterSidebarHandler(
    IPrinterRepository printerRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore,
    IPrinterStatusStream statusStream)
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
        // Track last sent runtime status per printer
        var lastSentByPrinter = new Dictionary<Guid, PrinterRuntimeStatus>();

        await foreach (var update in statusStream.Subscribe(workspaceId, ct))
        {
            var hasStateChange = update.RuntimeUpdate is not null;
            var hasPrinterChange = update.Printer is not null;
            if (!hasStateChange && !hasPrinterChange)
            {
                continue;
            }

            var printer = update.Printer ?? await printerRepository
                .GetByIdAsync(update.PrinterId, workspaceId, ct)
                .ConfigureAwait(false);
            if (printer is null)
            {
                continue;
            }

            var currentStatus = runtimeStatusStore.Get(printer.Id);

            // Build partial runtime update based on what changed since last send
            var lastSent = lastSentByPrinter.GetValueOrDefault(printer.Id);
            var runtimeUpdate = hasStateChange && update.RuntimeUpdate is not null
                ? BuildPartialRuntimeUpdate(lastSent, currentStatus)
                : null;

            // Skip if no actual changes to emit
            if (runtimeUpdate is null && !hasPrinterChange)
            {
                continue;
            }

            // Update last sent tracker
            if (currentStatus is not null)
            {
                lastSentByPrinter[printer.Id] = currentStatus;
            }

            yield return new PrinterSidebarSnapshot(printer, runtimeUpdate);
        }
    }

    private static PrinterRuntimeStatus? BuildPartialRuntimeUpdate(
        PrinterRuntimeStatus? lastSent,
        PrinterRuntimeStatus? current)
    {
        if (current is null)
        {
            return null;
        }

        // First time or no previous status - send all fields
        if (lastSent is null)
        {
            return current;
        }

        // Build partial update with only changed fields (null for unchanged)
        return new PrinterRuntimeStatus(
            current.PrinterId,
            State: current.State != lastSent.State ? current.State : null,
            UpdatedAt: current.UpdatedAt,
            BufferedBytes: current.BufferedBytes != lastSent.BufferedBytes ? current.BufferedBytes : null,
            Drawer1State: current.Drawer1State != lastSent.Drawer1State ? current.Drawer1State : null,
            Drawer2State: current.Drawer2State != lastSent.Drawer2State ? current.Drawer2State : null);
    }
}

