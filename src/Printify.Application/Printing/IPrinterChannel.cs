using System.Threading.Channels;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.Application.Printing;

/// <summary>
/// Transport abstraction for a bidirectional connection with a printer.
/// Implementations encapsulate the underlying socket/stream and surface
/// asynchronous read/write primitives.
/// <para>
/// <strong>Architecture Note:</strong> In this system, the printer application acts as the <strong>server</strong>,
/// and the physical printer device (or test client) acts as the <strong>client</strong>.
/// The client connects to the server to send print data and receive status responses.
/// </para>
/// </summary>
public interface IPrinterChannel : IAsyncDisposable
{
    /// <summary>
    /// Sends data from the server (printer application) to the client (physical printer device).
    /// This is used to send responses such as status bytes back to the client.
    /// </summary>
    /// <param name="data">The data to send to the client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// <strong>Direction:</strong> Server → Client (printer application sends to physical device)
    /// </remarks>
    ValueTask SendToClientAsync(ReadOnlyMemory<byte> data, CancellationToken ct);

    /// <summary>
    /// Printer that channel is related to
    /// </summary>
    Printer Printer { get; }

    /// <summary>
    /// Settings used for this channel's printer listener session.
    /// </summary>
    PrinterSettings Settings { get; }

    /// <summary>
    /// Address of the connected client.
    /// For TCP connections, this is "IP:port" (e.g., "192.168.1.100:52341").
    /// For in-memory channels, this is a test identifier (e.g., "memory://test-client-1").
    /// </summary>
    string ClientAddress { get; }

    /// <summary>
    /// Raised when data is received from the client (physical printer device or test simulator).
    /// This event fires when the client sends print data or commands to the server.
    /// </summary>
    /// <remarks>
    /// <strong>Direction:</strong> Client → Server (physical device sends to printer application)
    /// </remarks>
    event Func<IPrinterChannel, PrinterChannelDataEventArgs, ValueTask>? DataReceived;

    /// <summary>
    /// Raised once the channel has been closed, either gracefully or because of an error.
    /// </summary>
    event Func<IPrinterChannel, PrinterChannelClosedEventArgs, ValueTask>? Closed;
}
