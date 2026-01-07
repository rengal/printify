namespace Printify.Application.Printing;

/// <summary>
/// Snapshot of the simulated printer buffer at a point in time.
/// </summary>
public sealed record PrinterBufferSnapshot(
    int BufferedBytes,
    bool IsBusy,
    bool IsFull,
    bool IsEmpty);
