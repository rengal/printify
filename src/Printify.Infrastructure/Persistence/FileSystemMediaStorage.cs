using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Printify.Application.Interfaces;
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
    private readonly IServiceProvider serviceProvider;

    public FileSystemMediaStorage(
        IOptions<Storage> options,
        IServiceProvider serviceProvider)  // ← Inject service provider
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        
        this.serviceProvider = serviceProvider;
        
        var value = options.Value.MediaRootPath;
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
        var extension = DetermineFileExtension(upload.ContentType);
        var fileName = GetDataPath(blobId, extension);
        Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);

        await using (var file = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
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
            fileName,
            url);
    }

    public async ValueTask<Stream?> OpenReadAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var mediaRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        var media = await mediaRepository.GetMediaByIdAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (media is null || string.IsNullOrWhiteSpace(media.FileName))
        {
            return null;
        }

        if (!File.Exists(media.FileName))
        {
            return null;
        }

        Stream stream = new FileStream(media.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return stream;
    }

    public async ValueTask DeleteAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var mediaRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        var media = await mediaRepository.GetMediaByIdAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (media is null || string.IsNullOrWhiteSpace(media.FileName))
        {
            return;
        }

        TryDelete(media.FileName);
    }

    private string GetDataPath(string blobId, string extension)
    {
        var shards = GetShardPath(blobId);
        return Path.Combine(rootPath, shards.folder1, shards.folder2, blobId + "." + extension);
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

    private static string DetermineFileExtension(string contentType)
    {
        if (contentType.Contains("png", StringComparison.OrdinalIgnoreCase))
        {
            return "png";
        }

        if (contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("jpg", StringComparison.OrdinalIgnoreCase))
        {
            return "jpg";
        }

        if (contentType.Contains("gif", StringComparison.OrdinalIgnoreCase))
        {
            return "gif";
        }

        if (contentType.Contains("webp", StringComparison.OrdinalIgnoreCase))
        {
            return "webp";
        }

        return "bin";
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
