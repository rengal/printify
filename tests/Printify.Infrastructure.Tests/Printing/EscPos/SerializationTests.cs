using System.Text.Json;
using System.Text.Json.Serialization;
using Printify.Infrastructure.Mapping.Protocols.EscPos;
using Printify.Infrastructure.Persistence.Entities.Documents.EscPos;
using Printify.Infrastructure.Printing.EscPos.Commands;
using DomainMedia = Printify.Domain.Media.Media;

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

    [Theory]
    [MemberData(nameof(RasterImageCases))]
    public void RasterImageVariants_Roundtrip_WithMapper(
        EscPosRasterImage command,
        Type expectedPayloadType,
        Type expectedDomainType)
    {
        var payload = CommandMapper.ToCommandPayload(command);

        Assert.IsType(expectedPayloadType, payload);

        var roundtrip = Assert.IsAssignableFrom<EscPosRasterImage>(CommandMapper.ToDomain(payload, command.Media));
        Assert.Equal(expectedDomainType, roundtrip.GetType());
        Assert.Equal(command.Width, roundtrip.Width);
        Assert.Equal(command.Height, roundtrip.Height);
        Assert.Equal(command.Media.Id, roundtrip.Media.Id);
    }

    [Fact]
    public void LegacyRasterImagePayload_MapsToGs7630()
    {
        var media = DomainMedia.CreateDefaultPng(length: 10);
        var payload = new RasterImageElementPayload(Width: 8, Height: 2, media.Id);

        var command = Assert.IsType<EscPosRasterImageGs7630>(CommandMapper.ToDomain(payload, media));

        Assert.Equal(payload.Width, command.Width);
        Assert.Equal(payload.Height, command.Height);
        Assert.Equal(payload.MediaId, command.Media.Id);
    }

    public static TheoryData<EscPosRasterImage, Type, Type> RasterImageCases => new()
    {
        {
            new EscPosRasterImageGs7630(8, 2, DomainMedia.CreateDefaultPng(length: 10)),
            typeof(RasterImageGs7630ElementPayload),
            typeof(EscPosRasterImageGs7630)
        },
        {
            new EscPosRasterImageGs284C(8, 2, DomainMedia.CreateDefaultPng(length: 11)),
            typeof(RasterImageGs284CElementPayload),
            typeof(EscPosRasterImageGs284C)
        },
        {
            new EscPosRasterImageGs384C(8, 2, DomainMedia.CreateDefaultPng(length: 12)),
            typeof(RasterImageGs384CElementPayload),
            typeof(EscPosRasterImageGs384C)
        }
    };
}
