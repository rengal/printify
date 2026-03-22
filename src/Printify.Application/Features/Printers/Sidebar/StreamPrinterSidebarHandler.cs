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

        var workspaceId = request.Context.WorkspaceId.Value;
        // Subscribe eagerly so the channel is registered before the response headers are flushed.
        // Use CancellationToken.None here — the controller applies client-disconnect cancellation
        // via WithCancellation() when it iterates the result. The handler's cancellationToken is
        // scoped to the request dispatch and may fire before streaming completes.
        var subscription = statusStream.Subscribe(workspaceId, CancellationToken.None);
        var updates = ReadUpdatesAsync(workspaceId, subscription, CancellationToken.None);

        return new PrinterSidebarStreamResult("sidebar", updates);
    }

    private async IAsyncEnumerable<PrinterSidebarSnapshot> ReadUpdatesAsync(
        Guid workspaceId,
        IAsyncEnumerable<PrinterStatusUpdate> subscription,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Baseline snapshots sent to the stream (keyed by printer ID)
        var baselines = new Dictionary<Guid, PrinterSidebarSnapshot>();

        await foreach (var update in subscription.WithCancellation(ct))
        {
            // Only process updates with runtime changes or printer metadata changes
            if (update.RuntimeUpdate is null && update.Printer is null)
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

            // Get or create baseline for this printer.
            // Use null RuntimeStatus on first encounter so the first update always passes through.
            if (!baselines.TryGetValue(printer.Id, out var baseline))
            {
                baseline = new PrinterSidebarSnapshot(printer, null);
            }

            // Check for printer changes (sidebar only shows name and pin status)
            var printerChanged = printer.DisplayName != baseline.Printer.DisplayName ||
                printer.IsPinned != baseline.Printer.IsPinned;

            // Try to build partial runtime update
            var runtimeUpdate = update.RuntimeUpdate is not null
                ? TryBuildPartialRuntimeUpdate(currentStatus, baseline.RuntimeStatus, out var partialUpdate)
                    ? partialUpdate
                    : null
                : null;

            // Skip if nothing changed
            if (!printerChanged && runtimeUpdate is null)
            {
                continue;
            }

            // Update baseline with current state
            baselines[printer.Id] = new PrinterSidebarSnapshot(printer, currentStatus);

            yield return new PrinterSidebarSnapshot(printer, runtimeUpdate);
        }
    }

    private static bool TryBuildPartialRuntimeUpdate(
        PrinterRuntimeStatus? current,
        PrinterRuntimeStatus? baseline,
        out PrinterRuntimeStatus? partialUpdate)
    {
        partialUpdate = null;

        if (current is null)
        {
            return false;
        }

        // First time - send all fields
        if (baseline is null)
        {
            partialUpdate = current;
            return true;
        }

        // Sidebar only cares about State (Started/Stopped), ignore buffer/drawers
        var stateChanged = current.State != baseline.State;

        // If nothing changed, return false
        if (!stateChanged)
        {
            return false;
        }

        // Build partial update with only State
        partialUpdate = new PrinterRuntimeStatus(
            current.PrinterId,
            State: current.State,
            UpdatedAt: current.UpdatedAt,
            BufferedBytes: null,
            BufferedBytesDeltaBps: null,
            Drawer1State: null,
            Drawer2State: null);

        return true;
    }
}

