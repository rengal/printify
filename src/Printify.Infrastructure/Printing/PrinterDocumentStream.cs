using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Printify.Application.Printing;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterDocumentStream : IPrinterDocumentStream
{
    
    /// <summary>
    /// Stores active subscriptions organized by printer ID.
    /// Outer dictionary key: PrinterId (Guid) - identifies the printer.
    /// Inner dictionary key: SubscriptionId (Guid) - uniquely identifies each subscriber.
    /// Inner dictionary value: Channel for streaming <see cref="DocumentStreamEvent"/> to that specific subscriber.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<DocumentStreamEvent>>> subscriptions = new();
    private bool disposed;

    public IAsyncEnumerable<DocumentStreamEvent> Subscribe(Guid printerId, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var channel = Channel.CreateUnbounded<DocumentStreamEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = true
        });

        var subscriptionId = Guid.NewGuid();
        var bucket = subscriptions.GetOrAdd(printerId,
            static _ => new ConcurrentDictionary<Guid, Channel<DocumentStreamEvent>>());
        bucket[subscriptionId] = channel;

        return ReadChannelAsync(printerId, subscriptionId, channel, ct);
    }

    public void Publish(DocumentStreamEvent documentEvent)
    {
        if (disposed || !subscriptions.TryGetValue(documentEvent.Document.PrinterId, out var bucket))
        {
            return;
        }

        foreach (var channel in bucket.Values)
        {
            try
            {
                channel.Writer.TryWrite(documentEvent);
            }
            catch (ChannelClosedException)
            {
                // Channel already closed, ignore
            }
        }
    }

    private async IAsyncEnumerable<DocumentStreamEvent> ReadChannelAsync(
        Guid printerId,
        Guid subscriptionId,
        Channel<DocumentStreamEvent> channel,
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
            // Complete the writer synchronously to signal no more writes
            try
            {
                channel.Writer.Complete();
            }
            catch (Exception e)
            {
                Console.WriteLine(e); //todo debugnow
                // Writer may already be completed, ignore
            }

            if (subscriptions.TryGetValue(printerId, out var bucket))
            {
                bucket.TryRemove(subscriptionId, out _);
                if (bucket.IsEmpty)
                {
                    subscriptions.TryRemove(printerId, out _);
                }
            }
        }
    }
}
