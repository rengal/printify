namespace Printify.Listener;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Contracts.Service;

/// <summary>
/// Background service that listens for TCP connections and creates one tokenizer session per client.
/// </summary>
public sealed class ListenerService : BackgroundService, IListenerService
{
    private readonly ILogger<ListenerService> logger;
    private readonly ITokenizer tokenizer;
    private readonly IClockFactory clockFactory;
    private readonly ListenerOptions options;

    public ListenerService(
        ILogger<ListenerService> logger,
        ITokenizer tokenizer,
        IClockFactory clockFactory,
        IOptions<ListenerOptions> options)
    {
        this.logger = logger;
        this.tokenizer = tokenizer;
        this.clockFactory = clockFactory;
        this.options = options.Value;
    }

    /// <summary>
    /// Host calls StartAsync which in BackgroundService kicks off ExecuteAsync.
    /// Overriding allows this class to be discovered through IListenerService as well.
    /// </summary>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // Delegate to base to start ExecuteAsync in the background.
        return base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Host will call StopAsync on shutdown; override to ensure we propagate to base.
    /// </summary>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        // Delegate to base which signals ExecuteAsync to stop and waits for completion.
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, options.Port);
        listener.Start();
        logger.LogInformation("Listener started on port {Port}", options.Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Handle each client in background (fire-and-forget)
                _ = Task.Run(() => HandleClientAsync(client, stoppingToken), CancellationToken.None);
            }
        }
        finally
        {
            listener.Stop();
            logger.LogInformation("Listener stopped");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken hostCancellation)
    {
        // Create a dedicated clock and tokenizer session for this connection.
        var clock = clockFactory.Create();
        clock.Start();

        var sessionOptions = options.SessionOptions;
        var session = tokenizer.CreateSession(sessionOptions, clock);

        var stream = client.GetStream();
        var buffer = new byte[4096];
        // Track last activity in ms using the injected clock.
        var lastActivityMs = clock.ElapsedMs;
        var idleTimeoutMs = Math.Max(0, options.IdleTimeoutSeconds) * 1000L;

        try
        {
            // Read loop until client disconnects or host requested cancellation or idle timeout triggers.
            while (!hostCancellation.IsCancellationRequested && client.Connected)
            {
                var readTask = stream.ReadAsync(buffer.AsMemory(0, buffer.Length), hostCancellation);
                var completed = await Task.WhenAny(readTask, Task.Delay(1000, hostCancellation)).ConfigureAwait(false);

                if (completed == readTask)
                {
                    var bytesRead = await readTask.ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        // Client closed connection — mark session completed due to client disconnect.
                        logger.LogDebug("Client disconnected, completing session (ClientDisconnected)");
                        session.Complete(Printify.Contracts.Elements.CompletionReason.ClientDisconnected);
                        break;
                    }

                    // Feed bytes into tokenizer session.
                    session.Feed(buffer.AsSpan(0, bytesRead));
                    lastActivityMs = clock.ElapsedMs;
                    continue;
                }

                // Periodic tick: check idle timeout using injected clock.
                if (idleTimeoutMs > 0 && (clock.ElapsedMs - lastActivityMs) >= idleTimeoutMs)
                {
                    // Idle timeout reached — finalize session with DataTimeout.
                    logger.LogDebug("Idle timeout reached, completing session (DataTimeout)");
                    session.Complete(Printify.Contracts.Elements.CompletionReason.DataTimeout);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Host shutdown — ensure session is completed.
            try
            {
                session.Complete(Printify.Contracts.Elements.CompletionReason.DataTimeout);
            }
            catch
            {
                // ignore
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing client; completing session");
            try
            {
                session.Complete(Printify.Contracts.Elements.CompletionReason.DataTimeout);
            }
            catch
            {
                // ignore
            }
        }
        finally
        {
            try
            {
                client.Close();
            }
            catch
            {
                // ignore
            }
        }
    }
}
