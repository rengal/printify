using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Printify.Application.Printing;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// In-memory pub/sub stream for printer runtime status updates per workspace.
/// </summary>
public sealed class PrinterStatusStream : IPrinterStatusStream
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<PrinterStatusEvent>>> subscriptions = new();
    private bool disposed;

    public IAsyncEnumerable<PrinterStatusEvent> Subscribe(Guid workspaceId, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var channel = Channel.CreateUnbounded<PrinterStatusEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = true
        });

        var subscriptionId = Guid.NewGuid();
        var bucket = subscriptions.GetOrAdd(workspaceId,
            static _ => new ConcurrentDictionary<Guid, Channel<PrinterStatusEvent>>());
        bucket[subscriptionId] = channel;

        return ReadChannelAsync(workspaceId, subscriptionId, channel, ct);
    }

    public void Publish(PrinterStatusEvent statusEvent)
    {
        if (disposed || !subscriptions.TryGetValue(statusEvent.WorkspaceId, out var bucket))
        {
            return;
        }

        foreach (var channel in bucket.Values)
        {
            try
            {
                channel.Writer.TryWrite(statusEvent);
            }
            catch (ChannelClosedException)
            {
                // Channel already closed, ignore
            }
        }
    }

    private async IAsyncEnumerable<PrinterStatusEvent> ReadChannelAsync(
        Guid workspaceId,
        Guid subscriptionId,
        Channel<PrinterStatusEvent> channel,
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
