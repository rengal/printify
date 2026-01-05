namespace Printify.Domain.Printers;

/// <summary>
/// Target lifecycle state controlled by the operator.
/// </summary>
public enum PrinterTargetState
{
    Stopped = 0,
    Started = 1
}

/// <summary>
/// Observed state of the printer listener.
/// </summary>
public enum PrinterState
{
    Starting = 1,
    Started = 2,
    Stopped = 3,
    Error = 4
}

public enum DrawerState
{
    Closed = 0,
    OpenedManually = 1,
    OpenedByCommand = 2
}

/// <summary>
/// Controls which realtime fields are included in status streams.
/// </summary>
public enum PrinterRealtimeScope
{
    State = 0,
    Full = 1
}

/// <summary>
/// Full realtime status snapshot used for storage, API responses, and SSE payloads.
/// </summary>
public sealed record PrinterRealtimeStatus(
    Guid PrinterId,
    PrinterTargetState? TargetState,
    PrinterState? State,
    DateTimeOffset UpdatedAt,
    int? BufferedBytes = null,
    bool? IsCoverOpen = null,
    bool? IsPaperOut = null,
    bool? IsOffline = null,
    bool? HasError = null,
    bool? IsPaperNearEnd = null,
    DrawerState? Drawer1State = null,
    DrawerState? Drawer2State = null)
{
    // Minimal snapshot leaves optional fields unset so partial updates do not overwrite stored values.
    public PrinterRealtimeStatus(
        Guid printerId,
        PrinterTargetState targetState,
        PrinterState state,
        DateTimeOffset updatedAt)
        : this(printerId, targetState, state, updatedAt, null, null, null, null, null, null, null, null)
    {
    }
}

/// <summary>
/// Partial realtime status update; null fields indicate no change and should not overwrite stored values.
/// </summary>
public sealed record PrinterRealtimeStatusUpdate(
    Guid PrinterId,
    DateTimeOffset UpdatedAt,
    PrinterTargetState? TargetState = null,
    PrinterState? State = null,
    int? BufferedBytes = null,
    bool? IsCoverOpen = null,
    bool? IsPaperOut = null,
    bool? IsOffline = null,
    bool? HasError = null,
    bool? IsPaperNearEnd = null,
    DrawerState? Drawer1State = null,
    DrawerState? Drawer2State = null)
{
    // Apply a partial update to a snapshot to produce the latest view for publishing or responses.
    public PrinterRealtimeStatus ApplyTo(PrinterRealtimeStatus baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        return baseline with
        {
            TargetState = TargetState ?? baseline.TargetState,
            State = State ?? baseline.State,
            UpdatedAt = UpdatedAt,
            BufferedBytes = BufferedBytes ?? baseline.BufferedBytes,
            IsCoverOpen = IsCoverOpen ?? baseline.IsCoverOpen,
            IsPaperOut = IsPaperOut ?? baseline.IsPaperOut,
            IsOffline = IsOffline ?? baseline.IsOffline,
            HasError = HasError ?? baseline.HasError,
            IsPaperNearEnd = IsPaperNearEnd ?? baseline.IsPaperNearEnd,
            Drawer1State = Drawer1State ?? baseline.Drawer1State,
            Drawer2State = Drawer2State ?? baseline.Drawer2State
        };
    }
}
