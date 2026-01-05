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
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<PrinterRealtimeStatus>>> subscriptions = new();
    private bool disposed;

    public IAsyncEnumerable<PrinterRealtimeStatus> Subscribe(Guid workspaceId, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var channel = Channel.CreateUnbounded<PrinterRealtimeStatus>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = true
        });

        var subscriptionId = Guid.NewGuid();
        var bucket = subscriptions.GetOrAdd(workspaceId,
            static _ => new ConcurrentDictionary<Guid, Channel<PrinterRealtimeStatus>>());
        bucket[subscriptionId] = channel;

        return ReadChannelAsync(workspaceId, subscriptionId, channel, ct);
    }

    public void Publish(Guid workspaceId, PrinterRealtimeStatus status)
    {
        if (disposed || !subscriptions.TryGetValue(workspaceId, out var bucket))
        {
            return;
        }

        foreach (var channel in bucket.Values)
        {
            try
            {
                channel.Writer.TryWrite(status);
            }
            catch (ChannelClosedException)
            {
                // Channel already closed, ignore
            }
        }
    }

    private async IAsyncEnumerable<PrinterRealtimeStatus> ReadChannelAsync(
        Guid workspaceId,
        Guid subscriptionId,
        Channel<PrinterRealtimeStatus> channel,
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
