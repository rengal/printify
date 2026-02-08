using System.Globalization;
using System.Text;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Epl.Commands;

namespace Printify.Infrastructure.Printing.Epl;

public static class EplCommandHelper
{
    public static IReadOnlyList<string> GetDescription(Command command, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        // For EplCommand, delegate to the typed overload
        if (command is EplCommand eplCommand)
        {
            return GetDescription(eplCommand, culture);
        }

        return Lines(
            $"Unknown command ({command.GetType().Name})",
            $"Raw bytes length={command.RawBytes.Length}");
    }

    public static IReadOnlyList<string> GetDescription(EplCommand command, CultureInfo? culture = null)
    {
        return GetDescription(command, textEncoding: null, culture);
    }

    public static IReadOnlyList<string> GetDescription(
        EplCommand command,
        Encoding? textEncoding,
        CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command switch
        {
            EplParseError error => Lines(
                "Parser error",
                $"Code={error.Code}",
                $"Message=\"{EscapeDescriptionText(error.Message)}\""),
            EplPrinterError printerError => Lines(
                "Printer error",
                $"Message=\"{EscapeDescriptionText(printerError.Message)}\""),
            EplScalableText scalableText => BuildScalableTextDescription(
                scalableText,
                textEncoding ?? Encoding.GetEncoding(437)),
            EplDrawHorizontalLine horizontalLine => BuildDrawHorizontalLineDescription(horizontalLine),
            EplPrint print => BuildPrintDescription(print),
            EplDrawBox drawLine => BuildDrawLineDescription(drawLine),
            EplRasterImage rasterImage => BuildEplRasterImageDescription(rasterImage),
            EplPrintBarcode barcode => BuildEplPrintBarcodeDescription(barcode),
            EplClearBuffer => Lines("N - Clear buffer (acknowledge/clear image buffer)"),
            EplCarriageReturn => Lines("CR - Carriage return (0x0D)"),
            EplLineFeed => Lines("LF - Line feed (0x0A)"),
            EplSetLabelWidth labelWidth => Lines(
                "q width - Set label width",
                $"width={labelWidth.Width} (dots)"),
            EplSetLabelHeight labelHeight => Lines(
                "Q height, param2 - Set label height",
                $"height={labelHeight.Height} (dots)"),
            EplSetPrintSpeed speed => Lines(
                "R speed - Set print speed",
                $"speed={speed.Speed} (ips)"),
            EplSetPrintDarkness darkness => Lines(
                "S darkness - Set print darkness",
                $"darkness={darkness.Darkness}"),
            SetPrintDirection direction => Lines(
                "Z direction - Set print direction",
                $"direction={direction.Direction.ToString()}"),
            EplSetInternationalCharacter characterSet => Lines(
                "I p1,p2,p3 - Set international character set/codepage",
                $"p1={characterSet.P1}, p2={characterSet.P2}, p3={characterSet.P3}"),
            _ => Lines(
                $"Unknown command ({command.GetType().Name})",
                $"Raw bytes length={command.RawBytes.Length}")
        };
    }

    private static IReadOnlyList<string> BuildScalableTextDescription(
        EplScalableText eplScalableText,
        Encoding textEncoding)
    {
        var rotationLabel = eplScalableText.Rotation switch
        {
            0 => "normal",
            1 => "90°",
            2 => "180°",
            3 => "270°",
            _ => eplScalableText.Rotation.ToString()
        };

        return Lines(
            "A x,y,rotation,font,h,v,reverse,\"text\" - Scalable/rotatable text",
            $"x={eplScalableText.X}, y={eplScalableText.Y}",
            $"rotation={rotationLabel}",
            $"font={eplScalableText.Font}, h-mul={eplScalableText.HorizontalMultiplication}, v-mul={eplScalableText.VerticalMultiplication}",
            $"reverse={eplScalableText.Reverse}",
            $"text=\"{EscapeDescriptionText(textEncoding.GetString(eplScalableText.TextBytes))}\"");
    }

    private static IReadOnlyList<string> BuildDrawHorizontalLineDescription(EplDrawHorizontalLine horizontalLine)
    {
        return Lines(
            "LO x,y,thickness,length - Draw horizontal line",
            $"x={horizontalLine.X}, y={horizontalLine.Y}",
            $"thickness={horizontalLine.Thickness}, length={horizontalLine.Length}");
    }

    private static IReadOnlyList<string> BuildPrintDescription(EplPrint eplPrint)
    {
        return Lines(
            "P n - Print format and feed label",
            $"n={eplPrint.Copies} (copies)");
    }

    private static IReadOnlyList<string> BuildDrawLineDescription(EplDrawBox eplDrawBox)
    {
        return Lines(
            "X x1,y1,thickness,x2,y2 - Draw line or box",
            $"x1={eplDrawBox.X1}, y1={eplDrawBox.Y1}",
            $"thickness={eplDrawBox.Thickness}",
            $"x2={eplDrawBox.X2}, y2={eplDrawBox.Y2}");
    }

    private static IReadOnlyList<string> BuildEplRasterImageDescription(EplRasterImage eplRasterImage)
    {
        return Lines(
            "GW x,y,width,height,[data] - Raster image (finalized)",
            $"x={eplRasterImage.X}, y={eplRasterImage.Y}",
            $"width={eplRasterImage.Width} (dots), height={eplRasterImage.Height} (dots)",
            $"contentType={eplRasterImage.Media.ContentType}",
            $"url={eplRasterImage.Media.Url}");
    }

    private static IReadOnlyList<string> BuildEplPrintBarcodeDescription(EplPrintBarcode barcode)
    {
        var rotationLabel = barcode.Rotation switch
        {
            0 => "normal",
            1 => "90°",
            2 => "180°",
            3 => "270°",
            _ => barcode.Rotation.ToString()
        };

        return Lines(
            "B x,y,rotation,type,width,height,hri,\"data\" - Print barcode (finalized)",
            $"x={barcode.X}, y={barcode.Y}",
            $"rotation={rotationLabel}",
            $"type={barcode.Type}",
            $"width={barcode.Width}, height={barcode.Height}",
            $"hri={barcode.Hri}",
            $"data=\"{EscapeDescriptionText(barcode.Data)}\"",
            $"contentType={barcode.Media.ContentType}",
            $"url={barcode.Media.Url}");
    }

    private static IReadOnlyList<string> Lines(params string[] lines)
    {
        return lines;
    }

    private static string EscapeDescriptionText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
