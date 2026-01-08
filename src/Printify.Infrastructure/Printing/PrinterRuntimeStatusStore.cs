using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// In-memory runtime status store for active printers.
/// </summary>
public sealed class PrinterRuntimeStatusStore : IPrinterRuntimeStatusStore
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, PrinterRuntimeStatus> snapshots = new();

    public PrinterRuntimeStatus? Get(Guid printerId)
    {
        lock (gate)
        {
            return snapshots.TryGetValue(printerId, out var snapshot)
                ? snapshot
                : null;
        }
    }

    public PrinterRuntimeStatus Update(PrinterRuntimeStatusUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (gate)
        {
            if (!snapshots.TryGetValue(update.PrinterId, out var baseline))
            {
                // Seed a baseline so partial updates do not erase runtime defaults.
                baseline = new PrinterRuntimeStatus(
                    PrinterId: update.PrinterId,
                    State: update.State ?? PrinterState.Stopped,
                    UpdatedAt: update.UpdatedAt,
                    BufferedBytes: update.BufferedBytes ?? 0,
                    BufferedBytesDeltaBps: 0,
                    Drawer1State: update.Drawer1State ?? DrawerState.Closed,
                    Drawer2State: update.Drawer2State ?? DrawerState.Closed);
            }

            var updated = update.ApplyTo(baseline);
            snapshots[update.PrinterId] = updated;
            return updated;
        }
    }
}
