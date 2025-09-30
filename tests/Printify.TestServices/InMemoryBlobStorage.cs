using System.Collections.Concurrent;
using Printify.Contracts.Media;
using Printify.Contracts.Services;

namespace Printify.TestServices;

/// <summary>
/// Simple in-memory blob store used only for tests.
/// </summary>
public sealed class InMemoryBlobStorage : IBlobStorage
{
    private readonly ConcurrentDictionary<string, byte[]> store = new();
    private readonly ConcurrentDictionary<string, MediaDescriptor> metadataStore = new();

    public ValueTask<string> PutAsync(MediaContent media, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);
        ArgumentNullException.ThrowIfNull(media.Content);

        using var memoryStream = new MemoryStream();
        //media.Content.Value.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        var blobId = Guid.NewGuid().ToString("N");
        // store[blobId] = bytes;
        // metadataStore[blobId] = metadata with { ContentLength = bytes.LongLength };

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
        if (string.IsNullOrEmpty(blobId))
            throw new ArgumentException(nameof(blobId));

        store.TryRemove(blobId, out _);
        metadataStore.TryRemove(blobId, out _);

        return ValueTask.CompletedTask;
    }

    public bool TryGetMetadata(string blobId, out MediaDescriptor metadata)
    {
        return metadataStore.TryGetValue(blobId, out metadata!);
    }
}
