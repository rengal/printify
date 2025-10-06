using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Printify.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Documents;
using Printify.Contracts.Documents.Elements;
using Printify.Contracts.Media;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;

namespace Printify.Web.Tests;

public sealed class DocumentsControllerTests
{
    private const long DefaultPrinterId = 4242;

    [Fact]
    public async Task ListAsync_ReturnsSeededDocuments()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var auth = await LoginAsync(client, "DocUser");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var printerId = await CreatePrinterAsync(factory, "DocUser", "Alpha Printer");
        await SeedAsync(factory, CreateTextDocument("Alpha", "10.0.0.1", printerId));
        await SeedAsync(factory, CreateTextDocument("Beta", "10.0.0.2", printerId));

        var response = await client.GetAsync($"/api/documents?printerId={printerId}&limit=10");
        var payload = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, payload);

        using var json = JsonDocument.Parse(payload);
        var items = GetPropertyCaseInsensitive(json.RootElement, "Items");
        Assert.Equal(2, items.GetArrayLength());
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

        var auth = await LoginAsync(client, "DocRaster");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var printerId = await CreatePrinterAsync(factory, "DocRaster", "Raster Printer");
        var payload = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var id = await SeedAsync(factory, CreateRasterDocument(payload, printerId));

        var response = await client.GetAsync($"/api/documents/{id}?includeContent=true");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);

        using var json = JsonDocument.Parse(body);
        Assert.Equal(printerId, GetPropertyCaseInsensitive(json.RootElement, "PrinterId").GetInt64());
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

        var auth = await LoginAsync(client, "DocMedia");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var printerId = await CreatePrinterAsync(factory, "DocMedia", "Media Printer");
        var payload = Enumerable.Repeat((byte)0x5A, 16).ToArray();
        var id = await SeedAsync(factory, CreateRasterDocument(payload, printerId));

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

    private static async Task<long> SeedAsync(TestWebApplicationFactory factory, SaveDocumentRequest request)
    {
        using var scope = factory.Services.CreateScope();
        var commandService = scope.ServiceProvider.GetRequiredService<IResourceCommandService>();
        return await commandService.CreateDocumentAsync(request);
    }

    private static async Task<AuthResponse> LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!;
    }

    private static async Task<long> CreatePrinterAsync(TestWebApplicationFactory factory, string username, string displayName)
    {
        using var scope = factory.Services.CreateScope();
        var commandService = scope.ServiceProvider.GetRequiredService<IResourceCommandService>();
        var queryService = scope.ServiceProvider.GetRequiredService<IResourceQueryService>();
        var user = await queryService.FindUserByNameAsync(username);
        Assert.NotNull(user);
        return await commandService.CreatePrinterAsync(new SavePrinterRequest(user!.Id, displayName, "escpos", 384, null, "127.0.0.1"));
    }

    private static SaveDocumentRequest CreateTextDocument(string title, string? sourceIp, long printerId)
    {
        var elements = new Element[]
        {
            new TextLine(0, title)
        };

        return new SaveDocumentRequest(printerId, Protocol.EscPos, sourceIp, elements);
    }

    private static SaveDocumentRequest CreateRasterDocument(byte[] payload, long printerId)
    {
        var elements = new Element[]
        {
            new TextLine(0, "Receipt"),
            new RasterImageContent(1, 8, 4, new MediaContent(new MediaMeta("image/png", null, null), payload.AsMemory()))
        };

        return new SaveDocumentRequest(printerId, Protocol.EscPos, "127.0.0.1", elements);
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

    private sealed record LoginRequest(string Username);
    private sealed record AuthResponse(string Token, int ExpiresIn, UserResponse User);
    private sealed record UserResponse(long Id, string Name);
}