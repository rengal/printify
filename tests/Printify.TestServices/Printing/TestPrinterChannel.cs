using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.TestServices.Printing;

/// <summary>
/// In-memory printer channel used for integration tests. It relays written data through the <see cref="DataReceived"/> event so tests can observe payloads.
/// </summary>
public sealed class TestPrinterChannel(Printer printer, string clientAddress) : IPrinterChannel
{
    private bool isDisposed;

    public event Func<IPrinterChannel, PrinterChannelDataEventArgs, ValueTask>? DataReceived;

    public event Func<IPrinterChannel, PrinterChannelClosedEventArgs, ValueTask>? Closed;

    public Printer Printer { get; } = printer ?? throw new ArgumentNullException(nameof(printer));

    public string ClientAddress { get; } = clientAddress;

    public bool IsDisposed => isDisposed;

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (DataReceived is not null)
        {
            await DataReceived.Invoke(this, new PrinterChannelDataEventArgs(data, ct)).ConfigureAwait(false);
        }
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
