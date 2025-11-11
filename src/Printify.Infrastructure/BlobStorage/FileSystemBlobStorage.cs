using Microsoft.Extensions.Options;
using Printify.Domain.Config;
using Printify.Domain.Media;
using Printify.Domain.Services;

namespace Printify.Services.BlobStorage;

/// <summary>
/// File-system based blob storage that organises blobs in sharded directories.
/// </summary>
public sealed class FileSystemBlobStorage : IBlobStorage
{
    private readonly string rootPath;

    public FileSystemBlobStorage(IOptions<Storage> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value.BlobPath;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("RootPath must be configured", nameof(options));
        }

        rootPath = Path.GetFullPath(value);
        Directory.CreateDirectory(rootPath);
    }

    public async ValueTask<string> PutAsync(MediaUpload media, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);

        // var blobId = Guid.NewGuid().ToString("N");
        // var dataPath = GetDataPath(blobId);
        // Directory.CreateDirectory(Path.GetDirectoryName(dataPath)!);
        //
        // await using (var file = new FileStream(dataPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        // {
        //     await content.CopyToAsync(file, cancellationToken);
        // }
        //
        // var actualLength = new FileInfo(dataPath).Length;
        // var effectiveMetadata = metadata with { ContentLength = actualLength };
        // var metadataPath = GetMetadataPath(blobId);
        // await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(effectiveMetadata), cancellationToken);
        //
        // return blobId;
        return string.Empty; //debugnow
    }

    public ValueTask<Stream?> GetAsync(string blobId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blobId);
        var dataPath = GetDataPath(blobId);
        if (!File.Exists(dataPath))
        {
            return ValueTask.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return ValueTask.FromResult<Stream?>(stream);
    }

    public ValueTask DeleteAsync(string blobId, CancellationToken cancellationToken = default)
    {
        if (blobId != null)
        {
            var dataPath = GetDataPath(blobId);
            var metadataPath = GetMetadataPath(blobId);
            TryDelete(dataPath);
            TryDelete(metadataPath);
        }

        return ValueTask.CompletedTask;
    }

    private string GetDataPath(string blobId)
    {
        var shards = GetShardPath(blobId);
        return Path.Combine(rootPath, shards.folder1, shards.folder2, blobId + ".png");
    }

    private string GetMetadataPath(string blobId)
    {
        var shards = GetShardPath(blobId);
        return Path.Combine(rootPath, shards.folder1, shards.folder2, blobId + ".json");
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
