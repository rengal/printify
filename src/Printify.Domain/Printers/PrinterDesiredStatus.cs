namespace Printify.Domain.Printers;

/// <summary>
/// Desired lifecycle state controlled by the operator.
/// </summary>
public enum PrinterDesiredStatus
{
    Stopped = 0,
    Started = 1
}
