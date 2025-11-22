using System;
using System.IO;
using MediaRecord = Printify.Domain.Media.Media;
using MediaUpload = Printify.Domain.Media.MediaUpload;

namespace Printify.Domain.Services;

/// <summary>
/// Abstraction over persistent media storage (file system, object store, etc.).
/// Accepts transient uploads and materializes immutable <see cref="MediaRecord"/> records.
/// </summary>
public interface IMediaStorage
{
    /// <summary>
    /// Persists an uploaded payload and returns the resulting <see cref="MediaRecord"/> aggregate.
    /// </summary>
    ValueTask<MediaRecord> SaveAsync(MediaUpload upload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a read-only stream for the specified media. Returns <c>null</c> when not found.
    /// Caller is responsible for disposing the returned stream.
    /// </summary>
    ValueTask<Stream?> OpenReadAsync(Guid mediaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the stored media payload, if it exists.
    /// </summary>
    ValueTask DeleteAsync(Guid mediaId, CancellationToken cancellationToken = default);
}
