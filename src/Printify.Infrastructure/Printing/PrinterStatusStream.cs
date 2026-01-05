using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// In-memory pub/sub stream for printer realtime status updates per workspace.
/// </summary>
public sealed class PrinterStatusStream : IPrinterStatusStream
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<PrinterRealtimeStatusUpdate>>> subscriptions =
        new();
    private bool disposed;

    public IAsyncEnumerable<PrinterRealtimeStatusUpdate> Subscribe(Guid workspaceId, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var channel = Channel.CreateUnbounded<PrinterRealtimeStatusUpdate>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = true
        });

        var subscriptionId = Guid.NewGuid();
        var bucket = subscriptions.GetOrAdd(workspaceId,
            static _ => new ConcurrentDictionary<Guid, Channel<PrinterRealtimeStatusUpdate>>());
        bucket[subscriptionId] = channel;

        return ReadChannelAsync(workspaceId, subscriptionId, channel, ct);
    }

    public void Publish(Guid workspaceId, PrinterRealtimeStatusUpdate update)
    {
        if (disposed || !subscriptions.TryGetValue(workspaceId, out var bucket))
        {
            return;
        }

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

    private async IAsyncEnumerable<PrinterRealtimeStatusUpdate> ReadChannelAsync(
        Guid workspaceId,
        Guid subscriptionId,
        Channel<PrinterRealtimeStatusUpdate> channel,
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
