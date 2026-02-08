using Printify.Infrastructure.Mapping.Protocols.Epl;
using Printify.Infrastructure.Persistence.Entities.Documents.Epl;
using Printify.Infrastructure.Printing.Epl.Commands;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Printify.Infrastructure.Tests.Printing.Epl;

public class SerializationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ScalableTextElementPayload_Serialization_Roundtrip()
    {
        // Arrange - use primary constructor syntax
        var payload = new ScalableTextElementPayload(
            10,
            20,
            0,
            2,
            1,
            1,
            "N",
            "48656C6C6F" // "Hello" in hex
        );

        // Act - serialize to JSON
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        Console.WriteLine($"Serialized JSON: {json}");

        // Act - deserialize from JSON
        var deserialized = JsonSerializer.Deserialize<ScalableTextElementPayload>(json, SerializerOptions);

        // Assert
        Assert.NotNull(deserialized);
        Console.WriteLine($"Deserialized TextBytesHex: '{deserialized.TextBytesHex}'");
        Assert.Equal("48656C6C6F", deserialized.TextBytesHex);
        Assert.Equal(10, deserialized.X);
        Assert.Equal(20, deserialized.Y);
    }

    [Fact]
    public void EplRasterImageElementPayload_Serialization_Roundtrip()
    {
        // Arrange - use primary constructor syntax
        var mediaId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var payload = new EplRasterImageElementPayload(
            10,
            20,
            100,
            200,
            mediaId
        );

        // Act - serialize to JSON
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        Console.WriteLine($"Serialized JSON: {json}");

        // Act - deserialize from JSON
        var deserialized = JsonSerializer.Deserialize<EplRasterImageElementPayload>(json, SerializerOptions);

        // Assert
        Assert.NotNull(deserialized);
        Console.WriteLine($"Deserialized MediaId: '{deserialized.MediaId}'");
        Assert.Equal(mediaId, deserialized.MediaId);
        Assert.Equal(10, deserialized.X);
        Assert.Equal(20, deserialized.Y);
    }

    [Fact]
    public void ScalableText_Roundtrip_WithMapper()
    {
        // Arrange
        var textBytes = Encoding.GetEncoding(437).GetBytes("Hello");
        var command = new EplScalableText(10, 20, 0, 2, 1, 1, 'N', textBytes);

        // Act - convert to payload
        var payload = CommandMapper.ToCommandPayload(command);
        var textPayload = Assert.IsType<ScalableTextElementPayload>(payload);

        Console.WriteLine($"Original TextBytesHex: '{textPayload.TextBytesHex}'");
        Assert.Equal("48656C6C6F", textPayload.TextBytesHex);

        // Act - serialize to JSON
        var json = JsonSerializer.Serialize(textPayload, SerializerOptions);
        Console.WriteLine($"Serialized JSON: {json}");

        // Check if JSON contains textBytesHex
        Assert.Contains("textBytesHex", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("48656C6C6F", json);

        // Act - deserialize from JSON
        var deserialized = JsonSerializer.Deserialize<ScalableTextElementPayload>(json, SerializerOptions);

        // Assert
        Assert.NotNull(deserialized);
        Console.WriteLine($"Deserialized TextBytesHex: '{deserialized.TextBytesHex}'");
        Assert.Equal("48656C6C6F", deserialized.TextBytesHex);

        // Act - convert back to domain
        var roundtripCommand = CommandMapper.ToDomain(deserialized) as EplScalableText;
        Assert.NotNull(roundtripCommand);
        var roundtripText = Encoding.GetEncoding(437).GetString(roundtripCommand.TextBytes);
        Assert.Equal("Hello", roundtripText);
    }

    [Fact]
    public void ScalableText_Roundtrip_WithEmptyTextBytesHex()
    {
        // Arrange - test with empty string (not null)
        var payload = new ScalableTextElementPayload(
            10,
            20,
            0,
            2,
            1,
            1,
            "N",
            "" // Empty string
        );

        // Act - serialize to JSON
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        Console.WriteLine($"Serialized JSON (empty): {json}");

        // Act - deserialize from JSON
        var deserialized = JsonSerializer.Deserialize<ScalableTextElementPayload>(json, SerializerOptions);

        // Assert
        Assert.NotNull(deserialized);
        Console.WriteLine($"Deserialized TextBytesHex (empty): '{deserialized.TextBytesHex}'");
        Assert.Equal("", deserialized.TextBytesHex);

        // Act - convert back to domain
        var roundtripCommand = CommandMapper.ToDomain(deserialized) as EplScalableText;
        Assert.NotNull(roundtripCommand);
        Assert.Empty(roundtripCommand.TextBytes);
    }
}
