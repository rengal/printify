using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using Printify.Domain.Media;
using Printify.Domain.Services;

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

        if (!media.Content.HasValue)
        {
            throw new InvalidOperationException("Media content must include bytes when storing blobs.");
        }

        var bytes = media.Content.Value.ToArray();
        var blobId = Guid.NewGuid().ToString("N");

        var meta = EnsureMeta(media.Meta, bytes);

        store[blobId] = bytes;
        metadataStore[blobId] = new MediaDescriptor(meta, BuildUrl(blobId));

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
        if (string.IsNullOrWhiteSpace(blobId))
        {
            throw new ArgumentException(nameof(blobId));
        }

        store.TryRemove(blobId, out _);
        metadataStore.TryRemove(blobId, out _);

        return ValueTask.CompletedTask;
    }

    public bool TryGetMetadata(string blobId, out MediaDescriptor metadata)
    {
        return metadataStore.TryGetValue(blobId, out metadata!);
    }

    private static MediaMeta EnsureMeta(MediaMeta meta, byte[] content)
    {
        var length = meta.Length ?? content.LongLength;
        var checksum = meta.Checksum ?? Convert.ToHexString(SHA256.HashData(content));
        return meta with { Length = length, Checksum = checksum };
    }

    private static string BuildUrl(string blobId)
    {
        return $"/media/{blobId}";
    }
}
