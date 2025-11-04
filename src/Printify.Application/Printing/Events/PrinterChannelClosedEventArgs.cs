namespace Printify.Application.Printing.Events;

/// <summary>
/// Event payload emitted when a printer channel closes.
/// </summary>
public sealed record PrinterChannelClosedEventArgs(ChannelClosedReason Reason, CancellationToken CancellationToken);

public enum ChannelClosedReason
{
    Completed,
    Cancelled,
    Faulted
}
