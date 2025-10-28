using System.Net.Sockets;
using Printify.Application.Printing;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// TCP implementation of <see cref="IPrinterChannel"/>.
/// Provides asynchronous, bidirectional communication with a printer.
/// </summary>
public sealed class TcpPrinterChannel : IPrinterChannel
{
    private readonly TcpClient client;
    private NetworkStream? stream;
    private CancellationTokenSource? readLoopCts;
    private Task? readLoopTask;
    private volatile bool isStopped;

    public event Func<IPrinterChannel, PrinterChannelDataEventArgs, ValueTask>? DataReceived;
    public event Func<IPrinterChannel, PrinterChannelClosedEventArgs, ValueTask>? Closed;

    public TcpPrinterChannel(TcpClient client)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (stream != null)
            return; // already started

        stream = client.GetStream();
        readLoopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        readLoopTask = RunReadLoopAsync(readLoopCts.Token);

        await Task.CompletedTask; // non-blocking start
    }

    private async Task RunReadLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[4096]; // fallback initial buffer

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // If nothing available yet, read 1 byte to unblock
                int available = client.Available;
                int toRead = available > 0 ? available : 1;

                // Ensure buffer big enough
                if (buffer.Length < toRead)
                    buffer = new byte[toRead];

                int bytesRead = await stream!.ReadAsync(buffer.AsMemory(0, toRead), ct)
                    .ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    // remote printer closed connection gracefully
                    await OnClosedAsync(ChannelClosedReason.Completed).ConfigureAwait(false);
                    return;
                }

                // Drain any extra bytes immediately available in socket buffer
                while (client.Available > 0)
                {
                    int extra = client.Available;
                    if (buffer.Length < bytesRead + extra)
                        Array.Resize(ref buffer, bytesRead + extra);

                    int readNow = await stream.ReadAsync(
                        buffer.AsMemory(bytesRead, extra), ct).ConfigureAwait(false);

                    if (readNow == 0)
                    {
                        await OnClosedAsync(ChannelClosedReason.Completed).ConfigureAwait(false);
                        return;
                    }

                    bytesRead += readNow;
                }

                var data = new ReadOnlyMemory<byte>(buffer, 0, bytesRead);

                if (DataReceived != null)
                {
                    var args = new PrinterChannelDataEventArgs(data, ct);
                    await DataReceived.Invoke(this, args).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            await OnClosedAsync(ChannelClosedReason.Cancelled).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            await OnClosedAsync(ChannelClosedReason.Cancelled).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await OnClosedAsync(ChannelClosedReason.Faulted).ConfigureAwait(false);
        }
    }


    public async ValueTask WriteAsync(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (isStopped)
            throw new InvalidOperationException("Channel has been stopped.");

        if (stream == null)
            throw new InvalidOperationException("Channel not started.");

        try
        {
            await stream.WriteAsync(payload, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await OnClosedAsync(ChannelClosedReason.Cancelled).ConfigureAwait(false);
            throw;
        }
        catch
        {
            await OnClosedAsync(ChannelClosedReason.Faulted).ConfigureAwait(false);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (isStopped)
            return;

        isStopped = true;
        readLoopCts?.Cancel();

        if (readLoopTask != null)
        {
            try { await readLoopTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        await OnClosedAsync(ChannelClosedReason.Cancelled).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        try
        {
            stream?.Close();
            client.Close();
        }
        catch { /* ignore */ }

        stream?.Dispose();
        readLoopCts?.Dispose();
    }

    private async Task OnClosedAsync(ChannelClosedReason reason)
    {
        if (Closed != null)
        {
            var args = new PrinterChannelClosedEventArgs(reason);
            await Closed.Invoke(this, args).ConfigureAwait(false);
        }
    }
}
