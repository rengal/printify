namespace Printify.TestServcies.Storage;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Printify.Contracts.Service;

/// <summary>
/// Simple in-memory blob store used only for tests.
/// </summary>
public sealed class InMemoryBlobStorage : IBlobStorage
{
    private readonly ConcurrentDictionary<string, byte[]> store = new();
    private readonly ConcurrentDictionary<string, BlobMetadata> metadataStore = new();

    public ValueTask<string> PutAsync(Stream content, BlobMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(metadata);

        using var memoryStream = new MemoryStream();
        content.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        var blobId = Guid.NewGuid().ToString("N");
        store[blobId] = bytes;
        metadataStore[blobId] = metadata with { ContentLength = bytes.LongLength };

        return ValueTask.FromResult(blobId);
    }

    public ValueTask<Stream?> GetAsync(string blobId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blobId);

        if (store.TryGetValue(blobId, out var bytes))
        {
            return ValueTask.FromResult<Stream?>(new MemoryStream(bytes, writable: false));
        }

        return ValueTask.FromResult<Stream?>(null);
    }

    public ValueTask DeleteAsync(string blobId, CancellationToken cancellationToken = default)
    {
        if (blobId != null)
        {
            store.TryRemove(blobId, out _);
            metadataStore.TryRemove(blobId, out _);
        }

        return ValueTask.CompletedTask;
    }

    public bool TryGetMetadata(string blobId, out BlobMetadata metadata)
    {
        return metadataStore.TryGetValue(blobId, out metadata!);
    }
}
