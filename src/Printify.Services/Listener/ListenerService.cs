using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Printify.Contracts.Config;
using Printify.Contracts.Core;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Elements;
using Printify.Contracts.Services;

namespace Printify.Services.Listener;

/// <summary>
/// Background service that listens for TCP connections and creates one tokenizer session per client.
/// </summary>
public sealed class ListenerService : BackgroundService, IListenerService
{
    private readonly ITokenizer tokenizer;
    private readonly IClockFactory clockFactory;
    private readonly IRecordStorage recordStorage;
    private readonly ListenerOptions options;

    public ListenerService(
        ITokenizer tokenizer,
        IClockFactory clockFactory,
        IRecordStorage recordStorage,
        IOptions<ListenerOptions> options)
    {
        this.tokenizer = tokenizer;
        this.clockFactory = clockFactory;
        this.recordStorage = recordStorage;
        this.options = options.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, options.Port);
        listener.Start();
        Console.WriteLine($"Listener started on port {options.Port}");

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

                _ = Task.Run(() => HandleClientAsync(client, stoppingToken), CancellationToken.None);
            }
        }
        finally
        {
            listener.Stop();
            //logger.LogInformation("Listener stopped");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken hostCancellation)
    {
        var remoteEndpoint = client.Client.RemoteEndPoint as IPEndPoint;
        var remoteIp = remoteEndpoint?.Address.ToString(); // Capture client IP for document metadata.

        var clock = clockFactory.Create();
        clock.Start();

        var session = tokenizer.CreateSession();

        var stream = client.GetStream();
        var buffer = new byte[4096];
        var lastActivityMs = clock.ElapsedMs;
        var idleTimeoutMs = Math.Max(0, options.IdleTimeoutInMs);
        CompletionReason? completionReason = null;

        try
        {
            while (!hostCancellation.IsCancellationRequested && client.Connected)
            {
                var readTask = stream.ReadAsync(buffer.AsMemory(0, buffer.Length), hostCancellation).AsTask();
                var completed = await Task.WhenAny(readTask, Task.Delay(1000, hostCancellation)).ConfigureAwait(false);

                if (completed == readTask)
                {
                    var bytesRead = await readTask.ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        //logger.LogDebug("Client disconnected, completing session (ClientDisconnected)");
                        completionReason = CompletionReason.ClientDisconnected;
                        break;
                    }

                    session.Feed(buffer.AsSpan(0, bytesRead));
                    lastActivityMs = clock.ElapsedMs;
                    continue;
                }

                if (idleTimeoutMs > 0 && (clock.ElapsedMs - lastActivityMs) >= idleTimeoutMs)
                {
                    //logger.LogDebug("Idle timeout reached, completing session (DataTimeout)");
                    completionReason = CompletionReason.DataTimeout;
                    break;
                }
            }

            completionReason ??= CompletionReason.DataTimeout;
        }
        catch (OperationCanceledException)
        {
            completionReason ??= CompletionReason.DataTimeout;
        }
        catch (Exception ex)
        {
            //logger.LogError(ex, "Error while processing client; completing session");
            completionReason ??= CompletionReason.DataTimeout;
        }
        finally
        {
            if (completionReason.HasValue)
            {
                await FinalizeSessionAsync(session, completionReason.Value, remoteIp, hostCancellation).ConfigureAwait(false);
            }

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

    private async Task FinalizeSessionAsync(
        ITokenizerSession session,
        CompletionReason reason,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        try
        {
            session.Complete(reason);
        }
        catch (Exception ex)
        {
            //logger.LogWarning(ex, "Tokenizer session failed to complete");
            return;
        }

        if (!session.IsCompleted)
        {
            return;
        }

        Document? document;
        try
        {
            document = session.Document;
        }
        catch (Exception ex)
        {
            //logger.LogWarning(ex, "Tokenizer session did not expose a document");
            return;
        }

        if (document is null)
        {
            return;
        }

        var enrichedDocument = document with { Id = 0, SourceIp = remoteIp ?? document.SourceIp };

        try
        {
            await recordStorage.AddDocumentAsync(enrichedDocument, cancellationToken).ConfigureAwait(false); // Persist document for downstream retrieval tests and UI.
        }
        catch (Exception ex)
        {
            //logger.LogError(ex, "Failed to persist document to record storage");
        }
    }
}





