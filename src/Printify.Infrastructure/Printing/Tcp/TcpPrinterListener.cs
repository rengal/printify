using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing.Tcp;

public sealed class TcpPrinterListener(Printer printer, ILogger<TcpPrinterListener>? logger = null) : IPrinterListener
{
    private TcpListener? listener;
    private CancellationTokenSource? acceptLoopCts;
    private Task? backgroundTask;

    public event Func<IPrinterListener, PrinterChannelAcceptedEventArgs, ValueTask>? ChannelAccepted;

    public Guid PrinterId { get; } = printer.Id;
    public PrinterListenerStatus Status { get; private set; } = PrinterListenerStatus.Idle;

    public async Task StartAsync(CancellationToken ct)
    {
        if (Status is PrinterListenerStatus.Listening or PrinterListenerStatus.OpeningPort)
        {
            return;
        }

        Status = PrinterListenerStatus.OpeningPort;
        logger?.LogInformation("Starting TCP listener for printer {PrinterId} on port {Port}", printer.Id, printer.ListenTcpPortNumber);

        try
        {
            var endpoint = new IPEndPoint(IPAddress.Any, printer.ListenTcpPortNumber);
            listener = new TcpListener(endpoint);
            listener.Start();

            Status = PrinterListenerStatus.Listening;
            logger?.LogInformation("TCP listener is now active on port {Port}", printer.ListenTcpPortNumber);

            acceptLoopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            backgroundTask = RunAcceptLoopAsync(acceptLoopCts.Token);
            await Task.CompletedTask;
        }
        catch (OperationCanceledException)
        {
            Status = PrinterListenerStatus.Failed;
            throw;
        }
        catch (SocketException ex)
        {
            Status = PrinterListenerStatus.Failed;
            logger?.LogError(ex, "Failed to bind port {Port} for printer {PrinterId}", printer.ListenTcpPortNumber, printer.Id);
            throw;
        }
        catch (Exception ex)
        {
            Status = PrinterListenerStatus.Failed;
            logger?.LogError(ex, "Unexpected error starting listener on port {Port}", printer.ListenTcpPortNumber);
            throw;
        }
    }

    private async Task RunAcceptLoopAsync(CancellationToken ct)
    {
        if (listener == null)
        {
            throw new InvalidOperationException("Listener not initialized.");
        }

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                    logger?.LogInformation("Accepted new TCP connection for printer {PrinterId}", printer.Id);

                    var channel = new TcpPrinterChannel(printer, tcpClient);
                    if (ChannelAccepted != null)
                    {
                        var args = new PrinterChannelAcceptedEventArgs(printer.Id, channel);
                        await ChannelAccepted.Invoke(this, args).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
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
        {
            return;
        }

        logger?.LogInformation("Stopping TCP listener for printer {PrinterId}", printer.Id);
        Status = PrinterListenerStatus.Idle;

        try
        {
            acceptLoopCts?.Cancel();
            listener?.Stop();

            if (backgroundTask != null)
            {
                try
                {
                    await backgroundTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
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
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
