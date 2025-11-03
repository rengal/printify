using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Printify.Application.Printing;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterDocumentStream : IPrinterDocumentStream
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<DocumentStreamEvent>>> subscriptions = new();

    public IAsyncEnumerable<DocumentStreamEvent> Subscribe(Guid printerId, CancellationToken ct)
    {
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
        if (!subscriptions.TryGetValue(documentEvent.Document.PrinterId, out var bucket))
        {
            return;
        }

        foreach (var channel in bucket.Values)
        {
            channel.Writer.TryWrite(documentEvent);
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
            await foreach (var payload in channel.Reader.ReadAllAsync(ct))
            {
                yield return payload;
            }
        }
        finally
        {
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
