using System.Net.Sockets;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing.Tcp;

/// <summary>
/// TCP implementation of <see cref="IPrinterChannel"/> that streams data over an established <see cref="TcpClient"/>.
/// </summary>
public sealed class TcpPrinterChannel : IPrinterChannel
{
    private readonly TcpClient client;
    private readonly NetworkStream stream;
    private readonly CancellationTokenSource readLoopCts;
    private readonly Task readLoopTask;
    private int closeNotified;
    private bool disposed;

    public event Func<IPrinterChannel, PrinterChannelDataEventArgs, ValueTask>? DataReceived;
    public event Func<IPrinterChannel, PrinterChannelClosedEventArgs, ValueTask>? Closed;

    public TcpPrinterChannel(Printer printer, TcpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (!client.Connected)
        {
            throw new InvalidOperationException("The TCP client must be connected prior to channel construction.");
        }

        this.client = client;
        Printer = printer;
        ClientAddress = client.Client.RemoteEndPoint?.ToString() ?? string.Empty;
        stream = client.GetStream();
        readLoopCts = new CancellationTokenSource();
        readLoopTask = Task.Run(() => RunReadLoopAsync(readLoopCts.Token), readLoopCts.Token);
    }

    private async Task RunReadLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    await NotifyClosedAsync(ChannelClosedReason.Completed).ConfigureAwait(false);
                    return;
                }

                if (DataReceived != null)
                {
                    var args = new PrinterChannelDataEventArgs(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), ct);
                    await DataReceived.Invoke(this, args).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            await NotifyClosedAsync(ChannelClosedReason.Cancelled).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            await NotifyClosedAsync(ChannelClosedReason.Cancelled).ConfigureAwait(false);
        }
        catch
        {
            await NotifyClosedAsync(ChannelClosedReason.Faulted).ConfigureAwait(false);
        }
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        try
        {
            await stream.WriteAsync(data, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await NotifyClosedAsync(ChannelClosedReason.Cancelled).ConfigureAwait(false);
            throw;
        }
        catch
        {
            await NotifyClosedAsync(ChannelClosedReason.Faulted).ConfigureAwait(false);
            throw;
        }
    }

    public Printer Printer { get; }

    public string ClientAddress { get; }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        readLoopCts.Cancel();
        try
        {
            await readLoopTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        await NotifyClosedAsync(ChannelClosedReason.Cancelled).ConfigureAwait(false);

        stream.Dispose();
        client.Dispose();
        readLoopCts.Dispose();
    }

    private async Task NotifyClosedAsync(ChannelClosedReason reason)
    {
        if (Interlocked.CompareExchange(ref closeNotified, 1, 0) != 0)
        {
            return;
        }

        if (Closed != null)
        {
            var args = new PrinterChannelClosedEventArgs(reason);
            await Closed.Invoke(this, args).ConfigureAwait(false);
        }
    }
}
