namespace Printify.Domain.Printers;

/// <summary>
/// Partial runtime update published to status streams.
/// Null fields indicate no change and must not overwrite runtime state snapshots.
/// </summary>
public sealed record PrinterRuntimeStatusUpdate(
    Guid PrinterId,
    DateTimeOffset UpdatedAt,
    PrinterTargetState? TargetState = null,
    PrinterState? State = null,
    int? BufferedBytes = null,
    DrawerState? Drawer1State = null,
    DrawerState? Drawer2State = null);

public static class PrinterRuntimeStatusUpdateExtensions
{
    /// <summary>
    /// Applies a partial runtime update to a baseline snapshot.
    /// Null fields in the update retain the baseline value.
    /// </summary>
    public static PrinterRuntimeStatus ApplyTo(this PrinterRuntimeStatusUpdate update, PrinterRuntimeStatus baseline)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(baseline);

        return baseline with
        {
            State = update.State ?? baseline.State,
            UpdatedAt = update.UpdatedAt,
            BufferedBytes = update.BufferedBytes ?? baseline.BufferedBytes,
            Drawer1State = update.Drawer1State ?? baseline.Drawer1State,
            Drawer2State = update.Drawer2State ?? baseline.Drawer2State
        };
    }
}
