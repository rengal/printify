using Printify.Infrastructure.Mapping.Protocols.Epl;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.Documents.Epl;
using Printify.Infrastructure.Printing.Epl.Commands;
using DomainMedia = Printify.Domain.Media.Media;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Printify.Infrastructure.Tests.Printing.Epl;

/// <summary>
/// Debug test to trace EPL text through the full persistence cycle.
/// This test helps identify where TextBytesHex is lost during serialization/deserialization.
/// </summary>
public sealed class PersistenceDebugTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Trace_ScalableText_ThroughFullPersistenceCycle()
    {
        // Arrange - Create command with text
        var textBytes = Encoding.GetEncoding(437).GetBytes("Hello");
        var originalCommand = new EplScalableText(10, 20, 0, 2, 1, 1, 'N', textBytes)
        {
            LengthInBytes = 25
        };

        Console.WriteLine($"=== STEP 1: Original Command ===");
        Console.WriteLine($"  TextBytes (hex): '{Convert.ToHexString(originalCommand.TextBytes)}'");
        Console.WriteLine($"  TextBytes (len): {originalCommand.TextBytes.Length}");

        // Step 2: Convert to payload
        Console.WriteLine($"\n=== STEP 2: ToPayload ===");
        var payload = CommandMapper.ToCommandPayload(originalCommand);
        var textPayload = Assert.IsType<ScalableTextElementPayload>(payload);

        Console.WriteLine($"  TextBytesHex: '{textPayload.TextBytesHex}'");
        Console.WriteLine($"  Reverse: '{textPayload.Reverse}'");
        Assert.Equal("48656C6C6F", textPayload.TextBytesHex);

        // Step 3: Serialize to JSON (what gets stored in DB)
        Console.WriteLine($"\n=== STEP 3: Serialize to JSON ===");
        var json = JsonSerializer.Serialize(textPayload, SerializerOptions);
        Console.WriteLine($"  JSON: {json}");
        Console.WriteLine($"  JSON contains 'textBytesHex': {json.Contains("textBytesHex", StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"  JSON contains '48656C6C6F': {json.Contains("48656C6C6F")}");

        // Step 4: Deserialize from JSON (what gets read from DB)
        Console.WriteLine($"\n=== STEP 4: Deserialize from JSON ===");
        var deserializedPayload = JsonSerializer.Deserialize<ScalableTextElementPayload>(json, SerializerOptions);
        Assert.NotNull(deserializedPayload);

        Console.WriteLine($"  TextBytesHex: '{deserializedPayload.TextBytesHex}'");
        Console.WriteLine($"  TextBytesHex length: {deserializedPayload.TextBytesHex?.Length ?? 0}");

        // Step 5: Convert back to domain
        Console.WriteLine($"\n=== STEP 5: ToDomain ===");
        var roundtripCommand = CommandMapper.ToDomain(deserializedPayload);
        var roundtripTextCommand = Assert.IsType<EplScalableText>(roundtripCommand);

        Console.WriteLine($"  TextBytes (hex): '{Convert.ToHexString(roundtripTextCommand.TextBytes)}'");
        Console.WriteLine($"  TextBytes (len): {roundtripTextCommand.TextBytes.Length}");

        var decodedText = Encoding.GetEncoding(437).GetString(roundtripTextCommand.TextBytes);
        Console.WriteLine($"  Decoded text: '{decodedText}'");

        // Assert
        Assert.Equal("48656C6C6F", textPayload.TextBytesHex);
        Assert.Equal("48656C6C6F", deserializedPayload.TextBytesHex);
        Assert.Equal(textBytes, roundtripTextCommand.TextBytes);
        Assert.Equal("Hello", decodedText);
    }

    [Fact]
    public void Trace_EplRasterImage_ThroughFullPersistenceCycle()
    {
        // Arrange - Create command with media
        var mediaId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var media = new DomainMedia(
            Id: mediaId,
            OwnerWorkspaceId: null,
            CreatedAt: DateTimeOffset.UtcNow,
            IsDeleted: false,
            ContentType: "image/png",
            Length: 1234,
            Sha256Checksum: "abc123",
            FileName: "test.png",
            Url: "https://example.com/test.png"
        );
        var originalCommand = new EplRasterImage(10, 20, 100, 200, media)
        {
            LengthInBytes = 10
        };

        Console.WriteLine($"=== STEP 1: Original Command ===");
        Console.WriteLine($"  MediaId: '{originalCommand.Media.Id}'");
        Console.WriteLine($"  Url: '{originalCommand.Media.Url}'");

        // Step 2: Convert to payload
        Console.WriteLine($"\n=== STEP 2: ToPayload ===");
        var payload = CommandMapper.ToCommandPayload(originalCommand);
        var rasterPayload = Assert.IsType<EplRasterImageElementPayload>(payload);

        Console.WriteLine($"  MediaId: '{rasterPayload.MediaId}'");
        Assert.Equal(mediaId, rasterPayload.MediaId);

        // Step 3: Serialize to JSON
        Console.WriteLine($"\n=== STEP 3: Serialize to JSON ===");
        var json = JsonSerializer.Serialize(rasterPayload, SerializerOptions);
        Console.WriteLine($"  JSON: {json}");

        // Step 4: Deserialize from JSON
        Console.WriteLine($"\n=== STEP 4: Deserialize from JSON ===");
        var deserializedPayload = JsonSerializer.Deserialize<EplRasterImageElementPayload>(json, SerializerOptions);
        Assert.NotNull(deserializedPayload);

        Console.WriteLine($"  MediaId: '{deserializedPayload.MediaId}'");

        // Step 5: Convert back to domain (with media)
        Console.WriteLine($"\n=== STEP 5: ToDomain ===");
        var roundtripCommand = CommandMapper.ToDomain(deserializedPayload, media);
        var roundtripRasterCommand = Assert.IsType<EplRasterImage>(roundtripCommand);

        Console.WriteLine($"  MediaId: '{roundtripRasterCommand.Media.Id}'");
        Console.WriteLine($"  Url: '{roundtripRasterCommand.Media.Url}'");

        // Assert
        Assert.Equal(mediaId, rasterPayload.MediaId);
        Assert.Equal(mediaId, deserializedPayload.MediaId);
        Assert.Equal(mediaId, roundtripRasterCommand.Media.Id);
        Assert.Equal("https://example.com/test.png", roundtripRasterCommand.Media.Url);
    }
}
