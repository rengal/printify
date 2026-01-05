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
                    update.PrinterId,
                    update.State ?? PrinterState.Stopped,
                    update.UpdatedAt,
                    update.BufferedBytes ?? 0,
                    update.Drawer1State ?? DrawerState.Closed,
                    update.Drawer2State ?? DrawerState.Closed);
            }

            var updated = update.ApplyTo(baseline);
            snapshots[update.PrinterId] = updated;
            return updated;
        }
    }
}
