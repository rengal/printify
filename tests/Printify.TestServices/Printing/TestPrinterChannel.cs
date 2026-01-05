using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.TestServices.Printing;

/// <summary>
/// In-memory printer channel used for integration tests. It relays written data through the <see cref="DataReceived"/> event so tests can observe payloads.
/// </summary>
public sealed class TestPrinterChannel(Printer printer, PrinterSettings settings, string clientAddress) : IPrinterChannel
{
    private bool isDisposed;
    private readonly List<Action<ReadOnlyMemory<byte>>> responseHandlers = new();

    public event Func<IPrinterChannel, PrinterChannelDataEventArgs, ValueTask>? DataReceived;

    public event Func<IPrinterChannel, PrinterChannelClosedEventArgs, ValueTask>? Closed;

    public Printer Printer { get; } = printer ?? throw new ArgumentNullException(nameof(printer));

    public PrinterSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

    public string ClientAddress { get; } = clientAddress;

    public bool IsDisposed => isDisposed;

    /// <summary>
    /// Simulates the client (POS application) sending data to the server (virtual printer).
    /// This triggers the <see cref="IPrinterChannel.DataReceived"/> event to simulate
    /// a POS app sending print data or commands to the virtual printer.
    /// </summary>
    /// <remarks>Client → Server: Test helper that simulates POS app sending data to the virtual printer.</remarks>
    public async ValueTask SendToServerAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (DataReceived is not null)
        {
            await DataReceived.Invoke(this, new PrinterChannelDataEventArgs(data, ct)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends data from the server (virtual printer) to the test client (simulated POS app).
    /// Test handlers can capture this data via <see cref="OnResponse"/> to verify responses.
    /// </summary>
    /// <remarks>Server → Client: Simulates virtual printer sending status responses to the POS app.</remarks>
    public ValueTask SendToClientAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Notify response handlers (test captures responses here)
        foreach (var handler in responseHandlers)
        {
            handler(data);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Allows tests to capture responses from the printer.
    /// </summary>
    public void OnResponse(Action<ReadOnlyMemory<byte>> handler)
    {
        responseHandlers.Add(handler);
    }

    public async ValueTask CloseAsync(ChannelClosedReason reason)
    {
        if (Closed is null)
            return;

        await Closed.Invoke(this, new PrinterChannelClosedEventArgs(reason, CancellationToken.None)).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        isDisposed = true;
        return ValueTask.CompletedTask;
    }
}
