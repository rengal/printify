using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Elements;
using Printify.Contracts.Documents.Queries;
using Printify.Contracts.Documents.Services;
using Printify.Contracts.Media;
using Printify.Contracts.Services;

namespace Printify.Application.Documents.Queries;

/// <summary>
/// Provides read-side operations for documents and associated media.
/// </summary>
public sealed class ResourceQueryService : IResouceQueryService
{
    private readonly IRecordStorage recordStorage;
    private readonly IBlobStorage blobStorage;

    public ResourceQueryService(IRecordStorage recordStorage, IBlobStorage blobStorage)
    {
        ArgumentNullException.ThrowIfNull(recordStorage);
        ArgumentNullException.ThrowIfNull(blobStorage);

        this.recordStorage = recordStorage;
        this.blobStorage = blobStorage;
    }

    public async ValueTask<PagedResult<DocumentDescriptor>> ListAsync(ListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Limit), query.Limit, "Limit must be greater than zero.");
        }

        // Pull the page from record storage applying cursor and filters.
        var documents = await recordStorage.ListDocumentsAsync(query.Limit, query.BeforeId, query.SourceIp, cancellationToken).ConfigureAwait(false);

        var descriptors = documents
            .Select(CreateDescriptor)
            .ToList();

        var hasMore = documents.Count == query.Limit;
        long? nextBeforeId = hasMore ? documents[^1].Id : null;

        return new PagedResult<DocumentDescriptor>(descriptors, hasMore, nextBeforeId);
    }

    public async ValueTask<Document?> GetAsync(long id, bool includeContent = false, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Identifier must be positive.");
        }

        // Retrieve the stored document; return null when it is not present.
        var document = await recordStorage.GetDocumentAsync(id, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        if (!includeContent)
        {
            return document;
        }

        return await HydrateRasterContentAsync(document, cancellationToken).ConfigureAwait(false);
    }

    private static DocumentDescriptor CreateDescriptor(Document document)
    {
        var hasImages = document.Elements.Any(static e => e is BaseRasterImage);
        var previewText = document.Elements
            .OfType<TextLine>()
            .Select(static line => line.Text)
            .FirstOrDefault(static text => !string.IsNullOrWhiteSpace(text));

        return new DocumentDescriptor(
            document.Id,
            document.Timestamp,
            document.Protocol,
            document.SourceIp,
            document.Elements.Count,
            hasImages,
            previewText);
    }

    private async ValueTask<Document> HydrateRasterContentAsync(Document document, CancellationToken cancellationToken)
    {
        if (document.Elements.Count == 0)
        {
            return document;
        }

        var hydrated = new List<Element>(document.Elements.Count);
        var mutated = false;

        foreach (var element in document.Elements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (element is RasterImageDescriptor descriptor)
            {
                // Attempt to load the raster payload back into memory for render-heavy scenarios.
                var contentElement = await TryHydrateAsync(descriptor, cancellationToken).ConfigureAwait(false);
                if (contentElement is not null)
                {
                    hydrated.Add(contentElement);
                    mutated = true;
                    continue;
                }
            }

            hydrated.Add(element);
        }

        if (!mutated)
        {
            return document;
        }

        return document with { Elements = hydrated };
    }

    private async ValueTask<RasterImageContent?> TryHydrateAsync(
        RasterImageDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        if (!TryExtractBlobId(descriptor.Media.Url, out var blobId))
        {
            return null;
        }

        await using var stream = await blobStorage.GetAsync(blobId, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        // Materialize the blob bytes so downstream callers can work with in-memory media.
        var buffer = await ReadAllBytesAsync(stream, cancellationToken).ConfigureAwait(false);
        var meta = EnsureMeta(descriptor.Media.Meta, buffer);
        var media = new MediaContent(meta, buffer);

        return new RasterImageContent(descriptor.Sequence, descriptor.Width, descriptor.Height, media);
    }

    private static MediaMeta EnsureMeta(MediaMeta meta, byte[] content)
    {
        if (meta.Length.HasValue && meta.Checksum is not null)
        {
            return meta;
        }

        var length = meta.Length ?? content.LongLength;
        var checksum = meta.Checksum ?? Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
        return meta with { Length = length, Checksum = checksum };
    }

    private static async ValueTask<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return memory.ToArray();
    }

    private static bool TryExtractBlobId(string url, out string blobId)
    {
        blobId = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            return TryExtractFromPath(absolute.AbsolutePath, out blobId);
        }

        if (Uri.TryCreate(url, UriKind.Relative, out var relative))
        {
            return TryExtractFromPath(relative.OriginalString, out blobId);
        }

        return TryExtractFromPath(url, out blobId);
    }

    private static bool TryExtractFromPath(string path, out string blobId)
    {
        blobId = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        blobId = segments[^1];
        return !string.IsNullOrWhiteSpace(blobId);
    }
}
