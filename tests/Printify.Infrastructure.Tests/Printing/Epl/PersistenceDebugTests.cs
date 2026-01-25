using Printify.Infrastructure.Mapping.Protocols.Epl;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.Documents.Epl;
using Printify.Infrastructure.Printing.Epl.Commands;
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
        var originalCommand = new ScalableText(10, 20, 0, 2, 1, 1, 'N', textBytes)
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
        var roundtripTextCommand = Assert.IsType<ScalableText>(roundtripCommand);

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
    public void Trace_PrintGraphic_ThroughFullPersistenceCycle()
    {
        // Arrange - Create command with data
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var originalCommand = new PrintGraphic(10, 20, 100, 200, data)
        {
            LengthInBytes = 10
        };

        Console.WriteLine($"=== STEP 1: Original Command ===");
        Console.WriteLine($"  Data (hex): '{Convert.ToHexString(originalCommand.Data)}'");

        // Step 2: Convert to payload
        Console.WriteLine($"\n=== STEP 2: ToPayload ===");
        var payload = CommandMapper.ToCommandPayload(originalCommand);
        var graphicPayload = Assert.IsType<PrintGraphicElementPayload>(payload);

        Console.WriteLine($"  DataHex: '{graphicPayload.DataHex}'");
        Assert.Equal("01020304", graphicPayload.DataHex);

        // Step 3: Serialize to JSON
        Console.WriteLine($"\n=== STEP 3: Serialize to JSON ===");
        var json = JsonSerializer.Serialize(graphicPayload, SerializerOptions);
        Console.WriteLine($"  JSON: {json}");

        // Step 4: Deserialize from JSON
        Console.WriteLine($"\n=== STEP 4: Deserialize from JSON ===");
        var deserializedPayload = JsonSerializer.Deserialize<PrintGraphicElementPayload>(json, SerializerOptions);
        Assert.NotNull(deserializedPayload);

        Console.WriteLine($"  DataHex: '{deserializedPayload.DataHex}'");

        // Step 5: Convert back to domain
        Console.WriteLine($"\n=== STEP 5: ToDomain ===");
        var roundtripCommand = CommandMapper.ToDomain(deserializedPayload);
        var roundtripGraphicCommand = Assert.IsType<PrintGraphic>(roundtripCommand);

        Console.WriteLine($"  Data (hex): '{Convert.ToHexString(roundtripGraphicCommand.Data)}'");

        // Assert
        Assert.Equal("01020304", graphicPayload.DataHex);
        Assert.Equal("01020304", deserializedPayload.DataHex);
        Assert.Equal(data, roundtripGraphicCommand.Data);
    }
}
