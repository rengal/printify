namespace Printify.Contracts.Service;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Generic blob storage abstraction for persisting binary payloads.
/// Implementations may represent file systems, object stores, or databases.
/// </summary>
public interface IBlobStorage
{
    /// <summary>
    /// Stores a blob and returns an opaque identifier that can be used to retrieve it later.
    /// </summary>
    ValueTask<string> PutAsync(Stream content, BlobMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a blob’s content stream. Returns <c>null</c> if the blob does not exist.
    /// The caller is responsible for disposing the returned stream.
    /// </summary>
    ValueTask<Stream?> GetAsync(string blobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob. No-op if the blob cannot be found.
    /// </summary>
    ValueTask DeleteAsync(string blobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Descriptive metadata stored alongside a blob.
/// </summary>
/// <param name="ContentType">Mime type, e.g. image/png.</param>
/// <param name="ContentLength">Blob length in bytes.</param>
/// <param name="Checksum">Optional checksum hash (e.g. SHA256) for integrity validation.</param>
public sealed record BlobMetadata(
    string ContentType,
    long ContentLength,
    string? Checksum = null);
