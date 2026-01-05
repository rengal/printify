using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// In-memory pub/sub stream for printer status updates per workspace.
/// </summary>
public sealed class PrinterStatusStream : IPrinterStatusStream
{
    private readonly ILogger<PrinterStatusStream> logger;
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<PrinterStatusUpdate>>> subscriptions =
        new();
    private bool disposed;

    public PrinterStatusStream(ILogger<PrinterStatusStream> logger)
    {
        this.logger = logger;
    }

    public IAsyncEnumerable<PrinterStatusUpdate> Subscribe(Guid workspaceId, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var channel = Channel.CreateUnbounded<PrinterStatusUpdate>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = true
        });

        var subscriptionId = Guid.NewGuid();
        var bucket = subscriptions.GetOrAdd(workspaceId,
            static _ => new ConcurrentDictionary<Guid, Channel<PrinterStatusUpdate>>());
        bucket[subscriptionId] = channel;

        return ReadChannelAsync(workspaceId, subscriptionId, channel, ct);
    }

    public void Publish(Guid workspaceId, PrinterStatusUpdate update)
    {
        if (disposed || !subscriptions.TryGetValue(workspaceId, out var bucket))
        {
            // Diagnostic log to confirm publish attempts even without subscribers.
            logger.LogInformation("Printer status publish skipped (workspace {WorkspaceId}, printer {PrinterId}).",
                workspaceId, update.PrinterId);
            return;
        }

        // Diagnostic log for publish attempts to active subscribers.
        logger.LogInformation(
            "Printer status publish (workspace {WorkspaceId}, printer {PrinterId}, runtime {HasRuntime}, settings {HasSettings}, printer {HasPrinter}).",
            workspaceId,
            update.PrinterId,
            update.RuntimeUpdate is not null,
            update.Settings is not null,
            update.Printer is not null);

        foreach (var channel in bucket.Values)
        {
            try
            {
                channel.Writer.TryWrite(update);
            }
            catch (ChannelClosedException)
            {
                // Channel already closed, ignore
            }
        }
    }

    private async IAsyncEnumerable<PrinterStatusUpdate> ReadChannelAsync(
        Guid workspaceId,
        Guid subscriptionId,
        Channel<PrinterStatusUpdate> channel,
        [EnumeratorCancellation] CancellationToken ct)
    {
        try
        {
            await foreach (var payload in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                yield return payload;
            }
        }
        finally
        {
            try
            {
                channel.Writer.Complete();
            }
            catch
            {
                // ignore
            }

            if (subscriptions.TryGetValue(workspaceId, out var bucket))
            {
                bucket.TryRemove(subscriptionId, out _);
                if (bucket.IsEmpty)
                {
                    subscriptions.TryRemove(workspaceId, out _);
                }
            }
        }
    }
}
