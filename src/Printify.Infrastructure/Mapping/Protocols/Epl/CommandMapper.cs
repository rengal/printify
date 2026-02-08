using System.Text.Json;
using System.Text.Json.Serialization;
using Printify.Domain.Printing;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.Documents.Epl;
using Printify.Infrastructure.Printing.Epl.Commands;

namespace Printify.Infrastructure.Mapping.Protocols.Epl;

/// <summary>
/// Converts between EPL domain document commands and their serialized infrastructure representation.
/// </summary>
public static class CommandMapper
{
    private const EplPrintDirection DefaultPrintDirection = EplPrintDirection.TopToBottom;
    private const char DefaultCharReverse = 'N';

    public static EplDocumentElementPayload ToCommandPayload(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command switch
        {
            EplClearBuffer => new ClearBufferElementPayload(),
            EplCarriageReturn => new CarriageReturnElementPayload(),
            EplLineFeed => new LineFeedElementPayload(),
            EplSetLabelWidth labelWidth => new SetLabelWidthElementPayload(labelWidth.Width),
            EplSetLabelHeight labelHeight => new SetLabelHeightElementPayload(
                labelHeight.Height,
                labelHeight.SecondParameter),
            EplSetPrintSpeed speed => new SetPrintSpeedElementPayload(speed.Speed),
            EplSetPrintDarkness darkness => new SetPrintDarknessElementPayload(darkness.Darkness),
            SetPrintDirection direction => new SetPrintDirectionElementPayload(
                EnumMapper.ToString(direction.Direction)),
            EplSetInternationalCharacter intlChar => new SetInternationalCharacterElementPayload(intlChar.P1, intlChar.P2, intlChar.P3),
            PrinterError printerError => new PrinterErrorElementPayload(printerError.Message),
            EplScalableText text => new ScalableTextElementPayload(
                text.X,
                text.Y,
                text.Rotation,
                text.Font,
                text.HorizontalMultiplication,
                text.VerticalMultiplication,
                text.Reverse.ToString(),
                Convert.ToHexString(text.TextBytes)),
            EplDrawHorizontalLine line => new DrawHorizontalLineElementPayload(
                line.X,
                line.Y,
                line.Thickness,
                line.Length),
            EplDrawBox line => new DrawBoxElementPayload(
                line.X1,
                line.Y1,
                line.Thickness,
                line.X2,
                line.Y2),
            EplPrint print => new PrintElementPayload(print.Copies),
            PrintBarcode barcode => new PrintBarcodeElementPayload(
                barcode.X,
                barcode.Y,
                barcode.Rotation,
                barcode.Type,
                barcode.Width,
                barcode.Height,
                barcode.Hri.ToString(),
                barcode.Data),
            EplPrintBarcode barcode => new PrintBarcodeElementPayload(
                barcode.X,
                barcode.Y,
                barcode.Rotation,
                barcode.Type,
                barcode.Width,
                barcode.Height,
                barcode.Hri.ToString(),
                barcode.Data),
            EplRasterImage rasterImage => new EplRasterImageElementPayload(
                rasterImage.X,
                rasterImage.Y,
                rasterImage.Width,
                rasterImage.Height,
                rasterImage.Media.Id),
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
            ClearBufferElementPayload => new EplClearBuffer(),
            CarriageReturnElementPayload => new EplCarriageReturn(),
            LineFeedElementPayload => new EplLineFeed(),
            SetLabelWidthElementPayload labelWidth => new EplSetLabelWidth(labelWidth.Width),
            SetLabelHeightElementPayload labelHeight => new EplSetLabelHeight(
                labelHeight.Height,
                labelHeight.SecondParameter),
            SetPrintSpeedElementPayload speed => new EplSetPrintSpeed(speed.Speed),
            SetPrintDarknessElementPayload darkness => new EplSetPrintDarkness(darkness.Darkness),
            SetPrintDirectionElementPayload direction => new SetPrintDirection(
                EnumMapper.ParsePrintDirection(direction.Direction ?? "TopToBottom")),
            SetInternationalCharacterElementPayload intlChar => new EplSetInternationalCharacter(intlChar.P1, intlChar.P2, intlChar.P3),
            PrinterErrorElementPayload printerError => new PrinterError(printerError.Message ?? string.Empty),
            ScalableTextElementPayload text => new EplScalableText(
                text.X,
                text.Y,
                text.Rotation,
                text.Font,
                text.HorizontalMultiplication,
                text.VerticalMultiplication,
                string.IsNullOrEmpty(text.Reverse) ? DefaultCharReverse : text.Reverse[0],
                string.IsNullOrEmpty(text.TextBytesHex) ? Array.Empty<byte>() : Convert.FromHexString(text.TextBytesHex)),
            DrawHorizontalLineElementPayload line => new EplDrawHorizontalLine(
                line.X,
                line.Y,
                line.Thickness,
                line.Length),
            DrawBoxElementPayload line => new EplDrawBox(
                line.X1,
                line.Y1,
                line.Thickness,
                line.X2,
                line.Y2),
            PrintElementPayload print => new EplPrint(print.Copies),
            PrintBarcodeElementPayload barcode => media is not null
                ? new EplPrintBarcode(
                    barcode.X,
                    barcode.Y,
                    barcode.Rotation,
                    barcode.Type,
                    barcode.Width,
                    barcode.Height,
                    string.IsNullOrEmpty(barcode.Hri) ? DefaultCharReverse : barcode.Hri[0],
                    barcode.Data,
                    media)
                : new PrintBarcode(
                    barcode.X,
                    barcode.Y,
                    barcode.Rotation,
                    barcode.Type,
                    barcode.Width,
                    barcode.Height,
                    string.IsNullOrEmpty(barcode.Hri) ? DefaultCharReverse : barcode.Hri[0],
                    barcode.Data),
            EplRasterImageElementPayload raster => new EplRasterImage(
                raster.X,
                raster.Y,
                raster.Width,
                raster.Height,
                media),
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
            EplDocumentElementTypeNames.CarriageReturn => JsonSerializer.Deserialize<CarriageReturnElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.LineFeed => JsonSerializer.Deserialize<LineFeedElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetLabelWidth => JsonSerializer.Deserialize<SetLabelWidthElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetLabelHeight => JsonSerializer.Deserialize<SetLabelHeightElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetPrintSpeed => JsonSerializer.Deserialize<SetPrintSpeedElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetPrintDarkness => JsonSerializer.Deserialize<SetPrintDarknessElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetPrintDirection => JsonSerializer.Deserialize<SetPrintDirectionElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.SetInternationalCharacter => JsonSerializer.Deserialize<SetInternationalCharacterElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.PrinterError => JsonSerializer.Deserialize<PrinterErrorElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.ScalableText => JsonSerializer.Deserialize<ScalableTextElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.DrawHorizontalLine => JsonSerializer.Deserialize<DrawHorizontalLineElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.DrawLine => JsonSerializer.Deserialize<DrawBoxElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.Print => JsonSerializer.Deserialize<PrintElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.PrintBarcode => JsonSerializer.Deserialize<PrintBarcodeElementPayload>(entity.Payload, SerializerOptions),
            EplDocumentElementTypeNames.EplRasterImage => JsonSerializer.Deserialize<EplRasterImageElementPayload>(entity.Payload, SerializerOptions),
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
            CarriageReturnElementPayload => EplDocumentElementTypeNames.CarriageReturn,
            LineFeedElementPayload => EplDocumentElementTypeNames.LineFeed,
            SetLabelWidthElementPayload => EplDocumentElementTypeNames.SetLabelWidth,
            SetLabelHeightElementPayload => EplDocumentElementTypeNames.SetLabelHeight,
            SetPrintSpeedElementPayload => EplDocumentElementTypeNames.SetPrintSpeed,
            SetPrintDarknessElementPayload => EplDocumentElementTypeNames.SetPrintDarkness,
            SetPrintDirectionElementPayload => EplDocumentElementTypeNames.SetPrintDirection,
            SetInternationalCharacterElementPayload => EplDocumentElementTypeNames.SetInternationalCharacter,
            PrinterErrorElementPayload => EplDocumentElementTypeNames.PrinterError,
            ScalableTextElementPayload => EplDocumentElementTypeNames.ScalableText,
            DrawHorizontalLineElementPayload => EplDocumentElementTypeNames.DrawHorizontalLine,
            DrawBoxElementPayload => EplDocumentElementTypeNames.DrawLine,
            PrintElementPayload => EplDocumentElementTypeNames.Print,
            PrintBarcodeElementPayload => EplDocumentElementTypeNames.PrintBarcode,
            EplRasterImageElementPayload => EplDocumentElementTypeNames.EplRasterImage,
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }
}
