using Printify.Domain.Printing;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.Documents.Epl;
using Printify.Infrastructure.Printing.Epl.Commands;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Printify.Infrastructure.Mapping.Protocols.Epl;

/// <summary>
/// Converts between EPL domain document commands and their serialized infrastructure representation.
/// </summary>
public static class CommandMapper
{
    private const PrintDirection DefaultPrintDirection = PrintDirection.TopToBottom;
    private const char DefaultCharReverse = 'N';

    public static EplDocumentElementPayload ToCommandPayload(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command switch
        {
            ClearBuffer => new ClearBufferElementPayload(),
            SetLabelWidth labelWidth => new SetLabelWidthElementPayload(labelWidth.Width),
            SetLabelHeight labelHeight => new SetLabelHeightElementPayload(
                labelHeight.Height,
                labelHeight.SecondParameter),
            SetPrintSpeed speed => new SetPrintSpeedElementPayload(speed.Speed),
            SetPrintDarkness darkness => new SetPrintDarknessElementPayload(darkness.Darkness),
            SetPrintDirection direction => new SetPrintDirectionElementPayload(
                EnumMapper.ToString(direction.Direction)),
            SetInternationalCharacter intlChar => new SetInternationalCharacterElementPayload(intlChar.Code),
            SetCodePage codePage => new SetCodePageElementPayload(codePage.Code, codePage.Scaling),
            ScalableText text => new ScalableTextElementPayload(
                text.X,
                text.Y,
                text.Rotation,
                text.Font,
                text.HorizontalMultiplication,
                text.VerticalMultiplication,
                text.Reverse.ToString(),
                Convert.ToHexString(text.TextBytes)),
            DrawHorizontalLine line => new DrawHorizontalLineElementPayload(
                line.X,
                line.Y,
                line.Thickness,
                line.Length),
            DrawLine line => new DrawLineElementPayload(
                line.X1,
                line.Y1,
                line.Thickness,
                line.X2,
                line.Y2),
            Print print => new PrintElementPayload(print.Copies),
            PrintBarcode barcode => new PrintBarcodeElementPayload(
                barcode.X,
                barcode.Y,
                barcode.Rotation,
                barcode.Type,
                barcode.Width,
                barcode.Height,
                barcode.Hri.ToString(),
                barcode.Data),
            PrintGraphic graphic => new PrintGraphicElementPayload(
                graphic.X,
                graphic.Y,
                graphic.Width,
                graphic.Height,
                Convert.ToHexString(graphic.Data)),
            _ => throw new NotSupportedException($"Element type '{command.GetType().Name}' is not supported.")
        };
    }

    public static Command ToDomain(EplDocumentElementPayload dto)
    {
        return ToDomain(dto, null);
    }

    public static Command ToDomain(EplDocumentElementPayload dto, Domain.Media.Media? media)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return dto switch
        {
            ClearBufferElementPayload => new ClearBuffer(),
            SetLabelWidthElementPayload labelWidth => new SetLabelWidth(labelWidth.Width),
            SetLabelHeightElementPayload labelHeight => new SetLabelHeight(
                labelHeight.Height,
                labelHeight.SecondParameter),
            SetPrintSpeedElementPayload speed => new SetPrintSpeed(speed.Speed),
            SetPrintDarknessElementPayload darkness => new SetPrintDarkness(darkness.Darkness),
            SetPrintDirectionElementPayload direction => new SetPrintDirection(
                EnumMapper.ParsePrintDirection(direction.Direction ?? "TopToBottom")),
            SetInternationalCharacterElementPayload intlChar => new SetInternationalCharacter(intlChar.Code),
            SetCodePageElementPayload codePage => new SetCodePage(codePage.Code, codePage.Scaling),
            ScalableTextElementPayload text => new ScalableText(
                text.X,
                text.Y,
                text.Rotation,
                text.Font,
                text.HorizontalMultiplication,
                text.VerticalMultiplication,
                string.IsNullOrEmpty(text.Reverse) ? DefaultCharReverse : text.Reverse[0],
                string.IsNullOrEmpty(text.TextBytesHex) ? Array.Empty<byte>() : Convert.FromHexString(text.TextBytesHex)),
            DrawHorizontalLineElementPayload line => new DrawHorizontalLine(
                line.X,
                line.Y,
                line.Thickness,
                line.Length),
            DrawLineElementPayload line => new DrawLine(
                line.X1,
                line.Y1,
                line.Thickness,
                line.X2,
                line.Y2),
            PrintElementPayload print => new Print(print.Copies),
            PrintBarcodeElementPayload barcode => new PrintBarcode(
                barcode.X,
                barcode.Y,
                barcode.Rotation,
                barcode.Type,
                barcode.Width,
                barcode.Height,
                string.IsNullOrEmpty(barcode.Hri) ? DefaultCharReverse : barcode.Hri[0],
                barcode.Data),
            PrintGraphicElementPayload graphic => new PrintGraphic(
                graphic.X,
                graphic.Y,
                graphic.Width,
                graphic.Height,
                string.IsNullOrEmpty(graphic.DataHex) ? Array.Empty<byte>() : Convert.FromHexString(graphic.DataHex)),
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }

    internal static DocumentElementEntity ToEntity(
        Guid documentId,
        EplDocumentElementPayload dto,
        int sequence,
        byte[] rawBytes,
        int lengthInBytes)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(rawBytes);

        var json = JsonSerializer.Serialize(dto, dto.GetType(), SerializerOptions);

        return new DocumentElementEntity
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Sequence = sequence,
            ElementType = ResolveElementType(dto),
            Payload = json,
            CommandRaw = rawBytes.Length == 0 ? string.Empty : Convert.ToHexString(rawBytes),
            LengthInBytes = lengthInBytes
        };
    }

    internal static EplDocumentElementPayload? ToDto(DocumentElementEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (string.IsNullOrWhiteSpace(entity.Payload))
        {
            return null;
        }

        // Debug logging for ScalableText
        if (entity.ElementType == EplDocumentElementTypeNames.ScalableText)
        {
            System.Diagnostics.Debug.WriteLine($"[ToDto] Payload JSON: {entity.Payload}");
        }

        return entity.ElementType switch
        {
            EplDocumentElementTypeNames.ClearBuffer => JsonSerializer.Deserialize<ClearBufferElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetLabelWidth => JsonSerializer.Deserialize<SetLabelWidthElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetLabelHeight => JsonSerializer.Deserialize<SetLabelHeightElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetPrintSpeed => JsonSerializer.Deserialize<SetPrintSpeedElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetPrintDarkness => JsonSerializer.Deserialize<SetPrintDarknessElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetPrintDirection => JsonSerializer.Deserialize<SetPrintDirectionElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetInternationalCharacter => JsonSerializer.Deserialize<SetInternationalCharacterElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetCodePage => JsonSerializer.Deserialize<SetCodePageElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.ScalableText => JsonSerializer.Deserialize<ScalableTextElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.DrawHorizontalLine => JsonSerializer.Deserialize<DrawHorizontalLineElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.DrawLine => JsonSerializer.Deserialize<DrawLineElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.Print => JsonSerializer.Deserialize<PrintElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.PrintBarcode => JsonSerializer.Deserialize<PrintBarcodeElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.PrintGraphic => JsonSerializer.Deserialize<PrintGraphicElementPayload>(entity.Payload, SerializerOptions),
            _ => throw new NotSupportedException($"Element type '{entity.ElementType}' is not supported.")
        };
    }

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private static string ResolveElementType(EplDocumentElementPayload dto)
    {
        return dto switch
        {
            ClearBufferElementPayload => EplDocumentElementTypeNames.ClearBuffer,
            SetLabelWidthElementPayload => EplDocumentElementTypeNames.SetLabelWidth,
            SetLabelHeightElementPayload => EplDocumentElementTypeNames.SetLabelHeight,
            SetPrintSpeedElementPayload => EplDocumentElementTypeNames.SetPrintSpeed,
            SetPrintDarknessElementPayload => EplDocumentElementTypeNames.SetPrintDarkness,
            SetPrintDirectionElementPayload => EplDocumentElementTypeNames.SetPrintDirection,
            SetInternationalCharacterElementPayload => EplDocumentElementTypeNames.SetInternationalCharacter,
            SetCodePageElementPayload => EplDocumentElementTypeNames.SetCodePage,
            ScalableTextElementPayload => EplDocumentElementTypeNames.ScalableText,
            DrawHorizontalLineElementPayload => EplDocumentElementTypeNames.DrawHorizontalLine,
            DrawLineElementPayload => EplDocumentElementTypeNames.DrawLine,
            PrintElementPayload => EplDocumentElementTypeNames.Print,
            PrintBarcodeElementPayload => EplDocumentElementTypeNames.PrintBarcode,
            PrintGraphicElementPayload => EplDocumentElementTypeNames.PrintGraphic,
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }
}
