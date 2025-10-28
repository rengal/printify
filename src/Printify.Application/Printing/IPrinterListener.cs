using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.Application.Printing;

/// <summary>
/// Represents a single transport listener that accepts printer connections.
/// </summary>
public interface IPrinterListener : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Guid PrinterId { get; }
    PrinterListenerStatus Status { get; }

    /// <summary>
    /// Raised whenever a new channel has been accepted by the listener.
    /// </summary>
    event Func<IPrinterListener, PrinterChannelAcceptedEventArgs, ValueTask>? ChannelAccepted;
}
