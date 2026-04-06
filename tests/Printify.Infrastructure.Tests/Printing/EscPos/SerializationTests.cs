using System.Text.Json;
using System.Text.Json.Serialization;
using Printify.Infrastructure.Mapping.Protocols.EscPos;
using Printify.Infrastructure.Persistence.Entities.Documents.EscPos;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class SerializationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ParseError_Roundtrip_WithMapper()
    {
        var command = new EscPosParseError("ESCPOS_PARSER_ERROR", "Text contains non-ASCII bytes, but no code page was set.");

        var payload = CommandMapper.ToCommandPayload(command);
        var errorPayload = Assert.IsType<ErrorElementPayload>(payload);
        Assert.Equal(command.Code, errorPayload.Code);
        Assert.Equal(command.Message, errorPayload.Message);

        var json = JsonSerializer.Serialize(errorPayload, SerializerOptions);
        Assert.Contains("code", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("message", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(command.Message, json, StringComparison.Ordinal);

        var deserialized = JsonSerializer.Deserialize<ErrorElementPayload>(json, SerializerOptions);
        Assert.NotNull(deserialized);
        Assert.Equal(command.Code, deserialized.Code);
        Assert.Equal(command.Message, deserialized.Message);

        var roundtrip = Assert.IsType<EscPosParseError>(CommandMapper.ToDomain(deserialized));
        Assert.Equal(command.Code, roundtrip.Code);
        Assert.Equal(command.Message, roundtrip.Message);
    }
}
