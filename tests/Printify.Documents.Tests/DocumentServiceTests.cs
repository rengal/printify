using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Elements;
using Printify.Contracts.Documents.Queries;
using Printify.Contracts.Media;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.TestServices;

namespace Printify.Documents.Tests;

public sealed class DocumentServiceTests
{
    private const long DefaultPrinterId = 1001;

    [Fact]
    public async Task CreateDocumentAsync_OffloadsRasterToBlob()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var payload = Enumerable.Repeat((byte)0x5A, 32).ToArray();
        const long printerId = 123;
        var request = CreateDocument(payload, "Receipt A", printerId);

        var id = await commandService.CreateDocumentAsync(request);

        var stored = await context.RecordStorage.GetDocumentAsync(id);
        Assert.NotNull(stored);
        Assert.Equal(printerId, stored!.PrinterId);

        var descriptor = stored.Elements.OfType<RasterImageDescriptor>().Single();
        var expected = BuildDescriptorDocument(request, stored, payload, descriptor.Media.Url);
        DocumentAssertions.Equal(stored, expected);

        var blobId = descriptor.Media.Url.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        await using var blobStream = await context.BlobStorage.GetAsync(blobId);
        Assert.NotNull(blobStream);
        var blobBytes = await ReadAllAsync(blobStream!);
        Assert.Equal(payload, blobBytes);
    }

    [Fact]
    public async Task ListDocumentsAsync_ReturnsDescriptors()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();

        await commandService.CreateDocumentAsync(CreateDocument(Enumerable.Repeat((byte)0x11, 16).ToArray(), "Alpha", 201));
        await commandService.CreateDocumentAsync(CreateDocument(Enumerable.Repeat((byte)0x22, 24).ToArray(), "Beta", 202));

        var result = await queryService.ListDocumentsAsync(new ListQuery(10, null, null));

        Assert.Equal(2, result.Items.Count);
        Assert.False(result.HasMore);
        Assert.Null(result.NextBeforeId);
        Assert.Contains(result.Items, descriptor => descriptor.PreviewText == "Alpha");
        Assert.Contains(result.Items, descriptor => descriptor.PreviewText == "Beta");
    }

    [Fact]
    public async Task ListDocumentsAsync_PaginatesWithCursor()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();

        var titles = new[] { "First", "Second", "Third", "Fourth", "Fifth" };
        var ids = new long[titles.Length];

        for (var index = 0; index < titles.Length; index++)
        {
            ids[index] = await commandService.CreateDocumentAsync(CreateTextDocument(titles[index], printerId: index + 1));
        }

        var page1 = await queryService.ListDocumentsAsync(new ListQuery(2, null, null));
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.HasMore);
        Assert.Equal(ids[^1], page1.Items[0].Id);
        Assert.Equal(ids[^2], page1.Items[1].Id);

        var page2 = await queryService.ListDocumentsAsync(new ListQuery(2, page1.NextBeforeId, null));
        Assert.Equal(2, page2.Items.Count);
        Assert.True(page2.HasMore);
        Assert.Equal(ids[^3], page2.Items[0].Id);
        Assert.Equal(ids[^4], page2.Items[1].Id);

        var page3 = await queryService.ListDocumentsAsync(new ListQuery(2, page2.NextBeforeId, null));
        Assert.Single(page3.Items);
        Assert.False(page3.HasMore);
        Assert.Null(page3.NextBeforeId);
        Assert.Equal(ids[0], page3.Items[0].Id);
    }

    [Fact]
    public async Task ListDocumentsAsync_FiltersBySourceIp()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();

        await commandService.CreateDocumentAsync(CreateTextDocument("alpha", "10.0.0.1", 301));
        await commandService.CreateDocumentAsync(CreateTextDocument("beta", "10.0.0.2", 302));
        await commandService.CreateDocumentAsync(CreateTextDocument("gamma", "10.0.0.1", 303));

        var result = await queryService.ListDocumentsAsync(new ListQuery(10, null, "10.0.0.1"));

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, descriptor => Assert.Equal("10.0.0.1", descriptor.SourceIp));
    }

    [Fact]
    public async Task GetDocumentAsync_WithIncludeContent_HydratesRasterPayload()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();
        var payload = Enumerable.Range(0, 48).Select(value => (byte)value).ToArray();
        const long printerId = 555;
        var request = CreateDocument(payload, "Receipt B", printerId);

        var id = await commandService.CreateDocumentAsync(request);

        var reloaded = await queryService.GetDocumentAsync(id, includeContent: true);

        Assert.NotNull(reloaded);
        Assert.Equal(printerId, reloaded!.PrinterId);
        var raster = reloaded.Elements.OfType<RasterImageContent>().Single();
        Assert.True(raster.Media.Content.HasValue);
        Assert.Equal(payload.Length, raster.Media.Meta.Length);
        Assert.Equal(payload, raster.Media.Content.Value.ToArray());
    }

    private static Document BuildDescriptorDocument(SaveDocumentRequest request, Document stored, byte[] payload, string blobUrl)
    {
        var checksum = Convert.ToHexString(SHA256.HashData(payload));
        var textLine = request.Elements[0];
        var rasterRequest = request.Elements.OfType<RasterImageContent>().Single();
        var descriptor = new RasterImageDescriptor(
            rasterRequest.Sequence,
            rasterRequest.Width,
            rasterRequest.Height,
            new MediaDescriptor(new MediaMeta("image/png", payload.LongLength, checksum), blobUrl));

        var elements = new Element[] { textLine, descriptor };
        return new Document(stored.Id, stored.PrinterId, stored.Timestamp, stored.Protocol, stored.SourceIp, elements);
    }

    private static SaveDocumentRequest CreateDocument(byte[] payload, string title, long printerId, string? sourceIp = "127.0.0.1")
    {
        var elements = new Element[]
        {
            new TextLine(0, title),
            new RasterImageContent(1, 8, 8, new MediaContent(new MediaMeta("image/png", null, null), payload.AsMemory()))
        };

        return new SaveDocumentRequest(printerId, Protocol.EscPos, sourceIp, elements);
    }

    private static SaveDocumentRequest CreateTextDocument(string title, string? sourceIp = null, long printerId = DefaultPrinterId)
    {
        var elements = new Element[]
        {
            new TextLine(0, title)
        };

        return new SaveDocumentRequest(printerId, Protocol.EscPos, sourceIp, elements);
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        return memory.ToArray();
    }
}
