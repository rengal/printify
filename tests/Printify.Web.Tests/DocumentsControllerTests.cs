using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Elements;
using Printify.Contracts.Documents.Queries;
using Printify.Contracts.Documents.Services;
using Printify.Contracts.Media;

namespace Printify.Web.Tests;

public sealed class DocumentsControllerTests
{
    [Fact]
    public async Task ListAsync_ReturnsSeededDocuments()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        await SeedAsync(factory, CreateTextDocument("Alpha", "10.0.0.1"));
        await SeedAsync(factory, CreateTextDocument("Beta", "10.0.0.2"));

        var response = await client.GetAsync("/api/documents?limit=10");
        var payload = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, payload);

        using var json = JsonDocument.Parse(payload);
        var items = GetPropertyCaseInsensitive(json.RootElement, "Items");
        var previews = items.EnumerateArray()
            .Select(element => GetPropertyCaseInsensitive(element, "PreviewText").GetString())
            .ToArray();

        Assert.Contains("Alpha", previews);
        Assert.Contains("Beta", previews);
    }

    [Fact]
    public async Task GetAsync_WithIncludeContent_ReturnsRasterBytes()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var payload = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var id = await SeedAsync(factory, CreateRasterDocument(payload));

        var response = await client.GetAsync($"/api/documents/{id}?includeContent=true");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);

        using var json = JsonDocument.Parse(body);
        var elements = GetPropertyCaseInsensitive(json.RootElement, "Elements");
        var raster = elements.EnumerateArray()
            .First(element => GetPropertyCaseInsensitive(element, "Sequence").GetInt32() == 1);
        var media = GetPropertyCaseInsensitive(raster, "Media");
        var content = GetPropertyCaseInsensitive(media, "Content").GetString();
        Assert.NotNull(content);
        var bytes = Convert.FromBase64String(content!);
        Assert.Equal(payload, bytes);
    }

    [Fact]
    public async Task MediaEndpoint_StreamsBlob()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var payload = Enumerable.Repeat((byte)0x5A, 16).ToArray();
        var id = await SeedAsync(factory, CreateRasterDocument(payload));

        var descriptorResponse = await client.GetAsync($"/api/documents/{id}");
        var descriptorBody = await descriptorResponse.Content.ReadAsStringAsync();
        Assert.True(descriptorResponse.IsSuccessStatusCode, descriptorBody);

        using var json = JsonDocument.Parse(descriptorBody);
        var elements = GetPropertyCaseInsensitive(json.RootElement, "Elements");
        var raster = elements.EnumerateArray()
            .First(element => GetPropertyCaseInsensitive(element, "Sequence").GetInt32() == 1);
        var blobUrl = GetPropertyCaseInsensitive(GetPropertyCaseInsensitive(raster, "Media"), "Url").GetString();
        Assert.NotNull(blobUrl);
        var blobId = blobUrl!.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();

        var mediaResponse = await client.GetAsync($"/api/media/{blobId}");
        Assert.Equal(HttpStatusCode.OK, mediaResponse.StatusCode);
        var mediaPayload = await mediaResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(payload, mediaPayload);
    }

    private static async Task<long> SeedAsync(TestWebApplicationFactory factory, Document document)
    {
        using var scope = factory.Services.CreateScope();
        var commandService = scope.ServiceProvider.GetRequiredService<IResouceCommandService>();
        return await commandService.CreateAsync(document);
    }

    private static Document CreateTextDocument(string title, string? sourceIp)
    {
        var elements = new Element[]
        {
            new TextLine(0, title)
        };

        return new Document(0, DateTimeOffset.UtcNow, Protocol.EscPos, sourceIp, elements);
    }

    private static Document CreateRasterDocument(byte[] payload)
    {
        var elements = new Element[]
        {
            new TextLine(0, "Receipt"),
            new RasterImageContent(1, 8, 4, new MediaContent(new MediaMeta("image/png", null, null), payload.AsMemory()))
        };

        return new Document(0, DateTimeOffset.UtcNow, Protocol.EscPos, "127.0.0.1", elements);
    }

    private static JsonElement GetPropertyCaseInsensitive(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(name, out var value))
            {
                return value;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }
        }

        throw new KeyNotFoundException($"Property '{name}' not found. JSON: {element.GetRawText()}");
    }
}
