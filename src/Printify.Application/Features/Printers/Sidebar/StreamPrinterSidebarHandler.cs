using System.Runtime.CompilerServices;
using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Printers.Sidebar;

public sealed class StreamPrinterSidebarHandler(
    IPrinterRepository printerRepository,
    IWorkspaceRepository workspaceRepository,
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

        var workspaceId = PrinterAccess.RequireWorkspaceId(request.Context);
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workspace {workspaceId} not found.");
        // Subscribe eagerly so the channel is registered before the response headers are flushed.
        // Use CancellationToken.None here — the controller applies client-disconnect cancellation
        // via WithCancellation() when it iterates the result. The handler's cancellationToken is
        // scoped to the request dispatch and may fire before streaming completes.
        var subscription = workspace.Role == WorkspaceRole.Admin
            ? statusStream.SubscribeAll(CancellationToken.None)
            : statusStream.Subscribe(workspaceId, CancellationToken.None);
        var updates = ReadUpdatesAsync(workspaceId, workspace.Role == WorkspaceRole.Admin, subscription, CancellationToken.None);

        return new PrinterSidebarStreamResult("sidebar", updates);
    }

    private async IAsyncEnumerable<PrinterSidebarSnapshot> ReadUpdatesAsync(
        Guid workspaceId,
        bool includeAllWorkspaces,
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
                .GetByIdAsync(update.PrinterId, includeAllWorkspaces ? null : workspaceId, ct)
                .ConfigureAwait(false);
            if (printer is null)
            {
                continue;
            }

            var currentStatus = runtimeStatusStore.Get(printer.Id);
            var ownerWorkspace = await workspaceRepository.GetByIdAsync(printer.OwnerWorkspaceId, ct)
                .ConfigureAwait(false);

            // Get or create baseline for this printer.
            // Use null RuntimeStatus on first encounter so the first update always passes through.
            if (!baselines.TryGetValue(printer.Id, out var baseline))
            {
                baseline = new PrinterSidebarSnapshot(printer, null, ownerWorkspace?.Name);
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
            baselines[printer.Id] = new PrinterSidebarSnapshot(printer, currentStatus, ownerWorkspace?.Name);

            yield return new PrinterSidebarSnapshot(printer, runtimeUpdate, ownerWorkspace?.Name);
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

