namespace Printify.Domain.Printers;

/// <summary>
/// Observed runtime status of the printer listener.
/// </summary>
public enum PrinterRuntimeStatus
{
    Starting = 1,
    Started = 2,
    Stopped = 3,
    Error = 4
}
