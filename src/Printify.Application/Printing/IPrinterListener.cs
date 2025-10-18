using System;
using System.Threading;
using System.Threading.Tasks;

namespace Printify.Application.Printing;

/// <summary>
/// Represents a single transport listener that accepts printer connections.
/// </summary>
public interface IPrinterListener : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Raised whenever a new channel has been accepted by the listener.
    /// </summary>
    event Func<IPrinterListener, PrinterChannelAcceptedEventArgs, ValueTask> ChannelAccepted;
}

public sealed record PrinterChannelAcceptedEventArgs(Guid PrinterId, IPrinterChannel Channel);
