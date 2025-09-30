using Printify.Contracts.Media;

namespace Printify.Contracts.Services;

/// <summary>
/// Generic blob storage abstraction for persisting binary payloads.
/// Implementations may represent file systems, object stores, or databases.
/// </summary>
public interface IBlobStorage
{
    /// <summary>
    /// Stores a blob and returns an opaque identifier that can be used to retrieve it later.
    /// </summary>
    ValueTask<string> PutAsync(MediaContent media, CancellationToken cancellationToken = default);

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
