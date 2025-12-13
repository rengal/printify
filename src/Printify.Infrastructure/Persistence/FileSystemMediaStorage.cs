using Microsoft.Extensions.Options;
using Printify.Domain.Config;
using Printify.Domain.Media;
using Printify.Domain.Services;
using Printify.Infrastructure.Cryptography;

namespace Printify.Infrastructure.Persistence;

/// <summary>
/// File-system based media storage that persists uploads in sharded directories.
/// </summary>
public sealed class FileSystemMediaStorage : IMediaStorage
{
    private readonly string rootPath;

    public FileSystemMediaStorage(IOptions<Storage> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value.BlobPath;
        rootPath = string.IsNullOrWhiteSpace(value) ? string.Empty : Path.GetFullPath(value);
        if (!string.IsNullOrWhiteSpace(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }
    }

    public async ValueTask<Domain.Media.Media> SaveAsync(
        MediaUpload upload,
        Guid? ownerWorkspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);

        // Checksum is used for content-addressed deduplication; compute it once per upload where possible.
        var sha256Checksum = Sha256Checksum.ComputeLowerHex(upload.Content.Span);
        return await SaveAsync(upload, ownerWorkspaceId, sha256Checksum, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Domain.Media.Media> SaveAsync(
        MediaUpload upload,
        Guid? ownerWorkspaceId,
        string sha256Checksum,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (string.IsNullOrWhiteSpace(sha256Checksum))
        {
            // Guard against inconsistent callers; storage always persists a checksum for downstream lookups and caching.
            sha256Checksum = Sha256Checksum.ComputeLowerHex(upload.Content.Span);
        }

        var mediaId = Guid.NewGuid();
        var blobId = mediaId.ToString("N");
        var dataPath = GetDataPath(blobId);
        Directory.CreateDirectory(Path.GetDirectoryName(dataPath)!);

        await using (var file = new FileStream(dataPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                         useAsync: true))
        {
            if (!upload.Content.IsEmpty)
            {
                await file.WriteAsync(upload.Content, cancellationToken).ConfigureAwait(false);
            }
        }

        var url = BuildMediaUrl(mediaId);

        return new Domain.Media.Media(
            mediaId,
            ownerWorkspaceId,
            DateTimeOffset.UtcNow,
            IsDeleted: false,
            upload.ContentType,
            upload.Content.Length,
            sha256Checksum,
            url);
    }

    public ValueTask<Stream?> OpenReadAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var blobId = ToBlobId(mediaId);
        var dataPath = GetDataPath(blobId);
        if (!File.Exists(dataPath))
        {
            return ValueTask.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return ValueTask.FromResult<Stream?>(stream);
    }

    public ValueTask DeleteAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var blobId = ToBlobId(mediaId);
        var dataPath = GetDataPath(blobId);
        TryDelete(dataPath);
        return ValueTask.CompletedTask;
    }

    private string GetDataPath(string blobId)
    {
        var shards = GetShardPath(blobId);
        return Path.Combine(rootPath, shards.folder1, shards.folder2, blobId + ".bin");
    }

    private static string ToBlobId(Guid mediaId)
    {
        return mediaId.ToString("N");
    }

    private static string BuildMediaUrl(Guid mediaId)
    {
        return $"/api/media/{mediaId:D}";
    }

    private static (string folder1, string folder2) GetShardPath(string blobId)
    {
        if (blobId.Length < 4)
        {
            return ("00", "00");
        }

        return (blobId[..2], blobId[2..4]);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignored
        }
    }
}
