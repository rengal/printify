using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Elements;
using Printify.Contracts.Documents.Services;
using Printify.Contracts.Media;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.Contracts.Users;

namespace Printify.Application.Documents.Commands;

/// <summary>
/// Coordinates document persistence and media offloading to blob storage.
/// Also accepts user/printer registrations until dedicated services exist.
/// </summary>
public sealed class ResourceCommandService : IResouceCommandService
{
    private readonly IRecordStorage recordStorage;
    private readonly IBlobStorage blobStorage;

    public ResourceCommandService(IRecordStorage recordStorage, IBlobStorage blobStorage)
    {
        ArgumentNullException.ThrowIfNull(recordStorage);
        ArgumentNullException.ThrowIfNull(blobStorage);

        this.recordStorage = recordStorage;
        this.blobStorage = blobStorage;
    }

    public ValueTask<long> CreateUserAsync(SaveUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = new User(
            Id: 0,
            DisplayName: request.DisplayName,
            CreatedAt: DateTimeOffset.UtcNow,
            CreatedFromIp: request.CreatedFromIp);

        return recordStorage.AddUserAsync(user, cancellationToken);
    }

    public ValueTask<long> CreatePrinterAsync(SavePrinterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = new Printer(
            Id: 0,
            OwnerUserId: request.OwnerUserId,
            DisplayName: request.DisplayName,
            Protocol: request.Protocol,
            WidthInDots: request.WidthInDots,
            HeightInDots: request.HeightInDots,
            CreatedAt: DateTimeOffset.UtcNow,
            CreatedFromIp: request.CreatedFromIp);

        return recordStorage.AddPrinterAsync(printer, cancellationToken);
    }

    public async ValueTask<long> CreateDocumentAsync(SaveDocumentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var elements = await TransformElementsAsync(request.Elements, cancellationToken).ConfigureAwait(false);

        var document = new Document(
            Id: 0,
            PrinterId: request.PrinterId,
            Timestamp: DateTimeOffset.UtcNow,
            Protocol: request.Protocol,
            SourceIp: request.SourceIp,
            Elements: elements);

        return await recordStorage.AddDocumentAsync(document, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Element[]> TransformElementsAsync(
        IReadOnlyList<Element> source,
        CancellationToken cancellationToken)
    {
        if (source.Count == 0)
        {
            return Array.Empty<Element>();
        }

        var transformed = new List<Element>(source.Count);
        foreach (var element in source)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (element is RasterImageContent raster)
            {
                // Persist raster payloads to blob storage and store lightweight descriptors in documents.
                var descriptor = await PersistRasterAsync(raster, cancellationToken).ConfigureAwait(false);
                transformed.Add(descriptor);
                continue;
            }

            transformed.Add(element);
        }

        return transformed.ToArray();
    }

    private async ValueTask<RasterImageDescriptor> PersistRasterAsync(
        RasterImageContent raster,
        CancellationToken cancellationToken)
    {
        var media = raster.Media;
        var content = media.Content ?? throw new InvalidOperationException("Raster element is missing content bytes.");

        var ensuredMeta = EnsureMeta(media.Meta, content);
        var ensuredMedia = new MediaContent(ensuredMeta, content);

        // Store the raster bytes in blob storage to keep record storage lean.
        var blobId = await blobStorage.PutAsync(ensuredMedia, cancellationToken).ConfigureAwait(false);
        var descriptor = new MediaDescriptor(ensuredMeta, BuildBlobUrl(blobId));

        return new RasterImageDescriptor(raster.Sequence, raster.Width, raster.Height, descriptor);
    }

    private static MediaMeta EnsureMeta(MediaMeta meta, ReadOnlyMemory<byte> content)
    {
        if (meta.Length.HasValue && meta.Checksum is not null)
        {
            return meta;
        }

        // Guarantee downstream consumers know the payload size without fetching blobs.
        var length = meta.Length ?? content.Length;
        var checksum = meta.Checksum ?? ComputeSha256(content.Span);
        return meta with { Length = length, Checksum = checksum };
    }

    private static string ComputeSha256(ReadOnlySpan<byte> data)
    {
        Span<byte> buffer = stackalloc byte[32];
        System.Security.Cryptography.SHA256.HashData(data, buffer);
        return Convert.ToHexString(buffer);
    }

    private static string BuildBlobUrl(string blobId)
    {
        ArgumentNullException.ThrowIfNull(blobId);
        // Use an app-relative URL so HTTP handlers can later resolve the blob id.
        return $"/media/{blobId}";
    }
}
