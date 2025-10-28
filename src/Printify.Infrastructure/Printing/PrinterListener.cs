using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListener(Printer printer, ILogger<PrinterListener>? logger = null) : IPrinterListener
{
    private const int RetryDelayMs = 10000;
    private const int MaxRetries = 10;

    private TcpListener? listener;
    private CancellationTokenSource? acceptLoopCts;
    private Task? backgroundTask;

    public event Func<IPrinterListener, PrinterChannelAcceptedEventArgs, ValueTask>? ChannelAccepted;

    public PrinterListenerStatus Status { get; private set; } = PrinterListenerStatus.Idle;

    public async Task StartAsync(CancellationToken ct)
    {
        if (Status is PrinterListenerStatus.Listening or PrinterListenerStatus.OpeningPort)
            return;

        Status = PrinterListenerStatus.OpeningPort;
        logger?.LogInformation("Starting PrinterListener for printer {PrinterId} on port {Port}", printer.Id, printer.ListenTcpPortNumber);

        // Launch background retries without blocking StartAsync()
        backgroundTask = Task.Run(() => TryStartListeningLoopAsync(ct), ct);
        await Task.CompletedTask;
    }

    private async Task TryStartListeningLoopAsync(CancellationToken ct)
    {
        var endpoint = new IPEndPoint(IPAddress.Any, printer.ListenTcpPortNumber);
        var retries = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                listener = new TcpListener(endpoint);
                listener.Start();

                Status = PrinterListenerStatus.Listening;
                logger?.LogInformation("PrinterListener is now listening on port {Port}", printer.ListenTcpPortNumber);

                acceptLoopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                await RunAcceptLoopAsync(acceptLoopCts.Token).ConfigureAwait(false);
                return;
            }
            catch (SocketException ex) when (retries < MaxRetries)
            {
                retries++;
                Status = PrinterListenerStatus.OpeningPort;
                logger?.LogWarning(ex, "Retry {Retry}/{Max} to bind port {Port}", retries, MaxRetries, printer.ListenTcpPortNumber);
                await Task.Delay(RetryDelayMs, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unexpected error starting listener on port {Port}", printer.ListenTcpPortNumber);
                break;
            }
        }

        Status = PrinterListenerStatus.Failed;
        logger?.LogError("PrinterListener failed after {Retries} retries on port {Port}", retries, printer.ListenTcpPortNumber);
    }

    private async Task RunAcceptLoopAsync(CancellationToken ct)
    {
        if (listener == null)
            throw new InvalidOperationException("Listener not initialized.");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient? client = null;
                try
                {
                    client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                    logger?.LogInformation("Accepted new connection for printer {PrinterId}", printer.Id);

                    var channel = new TcpPrinterChannel(client);
                    if (ChannelAccepted != null)
                    {
                        var args = new PrinterChannelAcceptedEventArgs(printer.Id, channel);
                        await ChannelAccepted.Invoke(this, args).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // graceful shutdown
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error accepting client on port {Port}", printer.ListenTcpPortNumber);
                    await Task.Delay(500, ct).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            logger?.LogInformation("Accept loop ended for printer {PrinterId}", printer.Id);
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (Status == PrinterListenerStatus.Idle)
            return;

        logger?.LogInformation("Stopping PrinterListener for printer {PrinterId}", printer.Id);
        Status = PrinterListenerStatus.Idle;

        try
        {
            acceptLoopCts?.Cancel();
            listener?.Stop();

            if (backgroundTask != null)
            {
                try { await backgroundTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
        }
        finally
        {
            listener = null;
            acceptLoopCts?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }
}
