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

/// <summary>
/// Emulated cash drawer state tracked at runtime.
/// </summary>
public enum DrawerState
{
    Closed = 0,
    OpenedManually = 1,
    OpenedByCommand = 2
}

/// <summary>
/// Controls which runtime fields are included in status streams.
/// </summary>
public enum PrinterRealtimeScope
{
    State = 0,
    Full = 1
}
