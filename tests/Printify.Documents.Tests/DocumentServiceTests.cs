using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Elements;
using Printify.Contracts.Documents.Queries;
using Printify.Contracts.Documents.Services;
using Printify.Contracts.Media;
using Printify.TestServices;

namespace Printify.Documents.Tests;

public sealed class DocumentServiceTests
{
    [Fact]
    public async Task CreateAsync_OffloadsRasterToBlob()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResouceCommandService>();
        var payload = Enumerable.Repeat((byte)0x5A, 32).ToArray();
        var document = CreateDocument(payload, "Receipt A");

        var id = await commandService.CreateAsync(document);

        var stored = await context.RecordStorage.GetDocumentAsync(id);
        Assert.NotNull(stored);

        var expected = BuildDescriptorDocument(document, stored!.Id, payload, stored.Elements.OfType<RasterImageDescriptor>().Single().Media.Url);
        DocumentAssertions.Equal(stored, expected);

        var blobId = expected.Elements.OfType<RasterImageDescriptor>().Single().Media.Url.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        await using var blobStream = await context.BlobStorage.GetAsync(blobId);
        Assert.NotNull(blobStream);
        var blobBytes = await ReadAllAsync(blobStream!);
        Assert.Equal(payload, blobBytes);
    }

    [Fact]
    public async Task ListAsync_ReturnsDescriptors()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResouceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResouceQueryService>();

        await commandService.CreateAsync(CreateDocument(Enumerable.Repeat((byte)0x11, 16).ToArray(), "Alpha"));
        await commandService.CreateAsync(CreateDocument(Enumerable.Repeat((byte)0x22, 24).ToArray(), "Beta"));

        var result = await queryService.ListAsync(new ListQuery(10, null, null));

        Assert.Equal(2, result.Items.Count);
        Assert.False(result.HasMore);
        Assert.Null(result.NextBeforeId);
        Assert.Contains(result.Items, descriptor => descriptor.PreviewText == "Alpha");
        Assert.Contains(result.Items, descriptor => descriptor.PreviewText == "Beta");
    }

    [Fact]
    public async Task ListAsync_PaginatesWithCursor()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResouceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResouceQueryService>();

        var titles = new[] { "First", "Second", "Third", "Fourth", "Fifth" };
        var ids = new long[titles.Length];

        for (var index = 0; index < titles.Length; index++)
        {
            ids[index] = await commandService.CreateAsync(CreateTextDocument(titles[index]));
        }

        var page1 = await queryService.ListAsync(new ListQuery(2, null, null));
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.HasMore);
        Assert.Equal(ids[^1], page1.Items[0].Id);
        Assert.Equal(ids[^2], page1.Items[1].Id);

        var page2 = await queryService.ListAsync(new ListQuery(2, page1.NextBeforeId, null));
        Assert.Equal(2, page2.Items.Count);
        Assert.True(page2.HasMore);
        Assert.Equal(ids[^3], page2.Items[0].Id);
        Assert.Equal(ids[^4], page2.Items[1].Id);

        var page3 = await queryService.ListAsync(new ListQuery(2, page2.NextBeforeId, null));
        Assert.Single(page3.Items);
        Assert.False(page3.HasMore);
        Assert.Null(page3.NextBeforeId);
        Assert.Equal(ids[0], page3.Items[0].Id);
    }

    [Fact]
    public async Task ListAsync_FiltersBySourceIp()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResouceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResouceQueryService>();

        await commandService.CreateAsync(CreateTextDocument("alpha", "10.0.0.1"));
        await commandService.CreateAsync(CreateTextDocument("beta", "10.0.0.2"));
        await commandService.CreateAsync(CreateTextDocument("gamma", "10.0.0.1"));

        var result = await queryService.ListAsync(new ListQuery(10, null, "10.0.0.1"));

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, descriptor => Assert.Equal("10.0.0.1", descriptor.SourceIp));
    }

    [Fact]
    public async Task GetAsync_WithIncludeContent_HydratesRasterPayload()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResouceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResouceQueryService>();
        var payload = Enumerable.Range(0, 48).Select(value => (byte)value).ToArray();
        var document = CreateDocument(payload, "Receipt B");

        var id = await commandService.CreateAsync(document);

        var reloaded = await queryService.GetAsync(id, includeContent: true);

        Assert.NotNull(reloaded);
        var raster = reloaded!.Elements.OfType<RasterImageContent>().Single();
        Assert.True(raster.Media.Content.HasValue);
        Assert.Equal(payload.Length, raster.Media.Meta.Length);
        Assert.Equal(payload, raster.Media.Content.Value.ToArray());
    }

    private static Document BuildDescriptorDocument(Document source, long assignedId, byte[] payload, string blobUrl)
    {
        var checksum = Convert.ToHexString(SHA256.HashData(payload));
        var elements = new Element[]
        {
            source.Elements[0],
            new RasterImageDescriptor(1, 8, 8, new MediaDescriptor(new MediaMeta("image/png", payload.Length, checksum), blobUrl))
        };

        return source with { Id = assignedId, Elements = elements };
    }

    private static Document BuildContentDocument(Document source, long assignedId, byte[] payload)
    {
        var checksum = Convert.ToHexString(SHA256.HashData(payload));
        var elements = new Element[]
        {
            source.Elements[0],
            new RasterImageContent(1, 8, 8, new MediaContent(new MediaMeta("image/png", payload.Length, checksum), payload.AsMemory()))
        };

        return source with { Id = assignedId, Elements = elements };
    }

    private static Document CreateDocument(byte[] payload, string title)
    {
        var elements = new Element[]
        {
            new TextLine(0, title),
            new RasterImageContent(1, 8, 8, new MediaContent(new MediaMeta("image/png", null, null), payload.AsMemory()))
        };

        return new Document(0, DateTimeOffset.UtcNow, Protocol.EscPos, "127.0.0.1", elements);
    }

    private static Document CreateTextDocument(string title, string? sourceIp = null)
    {
        var elements = new Element[]
        {
            new TextLine(0, title)
        };

        return new Document(0, DateTimeOffset.UtcNow, Protocol.EscPos, sourceIp, elements);
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        return memory.ToArray();
    }
}
