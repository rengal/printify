using System;
using System.Threading;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.Application.Printing;

/// <summary>
/// Transport abstraction for a bidirectional connection with a printer.
/// Implementations encapsulate the underlying socket/stream and surface
/// asynchronous read/write primitives.
/// </summary>
public interface IPrinterChannel : IAsyncDisposable
{
    /// <summary>
    /// Sends a payload to the connected printer.
    /// </summary>
    ValueTask WriteAsync(ReadOnlyMemory<byte> payload, CancellationToken ct);

    /// <summary>
    /// Printer that channel is related to
    /// </summary>
    Printer Printer { get; }

    /// <summary>
    /// Raised when a chunk of data is received from the printer.
    /// </summary>
    event Func<IPrinterChannel, PrinterChannelDataEventArgs, ValueTask>? DataReceived;

    /// <summary>
    /// Raised once the channel has been closed, either gracefully or because of an error.
    /// </summary>
    event Func<IPrinterChannel, PrinterChannelClosedEventArgs, ValueTask>? Closed;
}
