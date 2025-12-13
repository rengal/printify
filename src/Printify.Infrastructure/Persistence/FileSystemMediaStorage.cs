using Microsoft.Extensions.Options;
using Printify.Domain.Config;
using Printify.Domain.Media;
using Printify.Domain.Services;
using System.Security.Cryptography;

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
        if (string.IsNullOrWhiteSpace(value))
        {
            //throw new ArgumentException("RootPath must be configured", nameof(options)); //todo debugnow
            rootPath = string.Empty;
        }

        //rootPath = Path.GetFullPath(value);
        //Directory.CreateDirectory(rootPath);
    }

    public async ValueTask<Domain.Media.Media> SaveAsync(MediaUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);

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

            await file.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        var sha256Checksum = Convert.ToHexString(SHA256.HashData(upload.Content.ToArray())).ToLowerInvariant(); //todo debugnow is effective?
        var url = BuildMediaUrl(mediaId);

        return new Domain.Media.Media(
            mediaId,
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
