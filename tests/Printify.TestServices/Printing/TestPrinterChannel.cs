using System;
using System.Threading;
using System.Threading.Tasks;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.TestServices.Printing;

/// <summary>
/// In-memory printer channel used for integration tests. It relays written data through the <see cref="DataReceived"/> event so tests can observe payloads.
/// </summary>
public sealed class TestPrinterChannel : IPrinterChannel
{
    private bool isDisposed;

    public TestPrinterChannel(Printer printer, string clientAddress)
    {
        Printer = printer ?? throw new ArgumentNullException(nameof(printer));
        ClientAddress = clientAddress;
    }

    public event Func<IPrinterChannel, PrinterChannelDataEventArgs, ValueTask>? DataReceived;

    public event Func<IPrinterChannel, PrinterChannelClosedEventArgs, ValueTask>? Closed;

    public Printer Printer { get; }

    public string ClientAddress { get; }

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

        await Closed.Invoke(this, new PrinterChannelClosedEventArgs(reason)).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        isDisposed = true;
        return ValueTask.CompletedTask;
    }
}
