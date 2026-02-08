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
            $"Raw bytes length={command.RawBytes?.Length ?? 0}");
    }

    public static IReadOnlyList<string> GetDescription(EplCommand command, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        // TODO: Use culture parameter for future localization

        // Keep command descriptions short and stable for UI/debug consumers.
        return command switch
        {
            EplParseError error => Lines(
                "Parser error",
                $"Code={error.Code}",
                $"Message=\"{EscapeDescriptionText(error.Message)}\""),
            EplPrinterError printerError => Lines(
                "Printer error",
                $"Message=\"{EscapeDescriptionText(printerError.Message)}\""),
            ScalableText scalableText => BuildScalableTextDescription(scalableText),
            DrawHorizontalLine horizontalLine => BuildDrawHorizontalLineDescription(horizontalLine),
            Print print => BuildPrintDescription(print),
            PrintBarcode eplBarcode => BuildEplBarcodeDescription(eplBarcode),
            PrintGraphic graphic => BuildPrintGraphicDescription(graphic),
            DrawBox drawLine => BuildDrawLineDescription(drawLine),
            EplRasterImage rasterImage => BuildEplRasterImageDescription(rasterImage),
            EplPrintBarcode barcode => BuildEplPrintBarcodeDescription(barcode),
            ClearBuffer => Lines("N - Clear buffer (acknowledge/clear image buffer)"),
            CarriageReturn => Lines("CR - Carriage return (0x0D)"),
            LineFeed => Lines("LF - Line feed (0x0A)"),
            SetLabelWidth labelWidth => Lines(
                "q width - Set label width",
                $"width={labelWidth.Width} (dots)"),
            SetLabelHeight labelHeight => Lines(
                "Q height, param2 - Set label height",
                $"height={labelHeight.Height} (dots)"),
            SetPrintSpeed speed => Lines(
                "R speed - Set print speed",
                $"speed={speed.Speed} (ips)"),
            SetPrintDarkness darkness => Lines(
                "S darkness - Set print darkness",
                $"darkness={darkness.Darkness}"),
            SetPrintDirection direction => Lines(
                "Z direction - Set print direction",
                $"direction={direction.Direction.ToString()}"),
            SetInternationalCharacter intlChar => Lines(
                "I p1,p2,p3 - Set international character set/codepage",
                $"p1={intlChar.P1}, p2={intlChar.P2}, p3={intlChar.P3}"),
            _ => Lines(
                $"Unknown command ({command.GetType().Name})",
                $"Raw bytes length={command.RawBytes?.Length ?? 0}")
        };
    }

    private static IReadOnlyList<string> BuildScalableTextDescription(ScalableText scalableText)
    {
        var rotationLabel = scalableText.Rotation switch
        {
            0 => "normal",
            1 => "90°",
            2 => "180°",
            3 => "270°",
            _ => scalableText.Rotation.ToString()
        };

        return Lines(
            "A x,y,rotation,font,h,v,reverse,\"text\" - Scalable/rotatable text",
            $"x={scalableText.X}, y={scalableText.Y}",
            $"rotation={rotationLabel}",
            $"font={scalableText.Font}, h-mul={scalableText.HorizontalMultiplication}, v-mul={scalableText.VerticalMultiplication}",
            $"reverse={scalableText.Reverse}",
            $"text=\"{EscapeDescriptionText(Encoding.GetEncoding(437).GetString(scalableText.TextBytes))}\"");
    }

    private static IReadOnlyList<string> BuildDrawHorizontalLineDescription(DrawHorizontalLine horizontalLine)
    {
        return Lines(
            "LO x,y,thickness,length - Draw horizontal line",
            $"x={horizontalLine.X}, y={horizontalLine.Y}",
            $"thickness={horizontalLine.Thickness}, length={horizontalLine.Length}");
    }

    private static IReadOnlyList<string> BuildPrintDescription(Print print)
    {
        return Lines(
            "P n - Print format and feed label",
            $"n={print.Copies} (copies)");
    }

    private static IReadOnlyList<string> BuildEplBarcodeDescription(PrintBarcode barcode)
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
            "B x,y,rotation,type,width,height,hri,\"data\" - Print barcode",
            $"x={barcode.X}, y={barcode.Y}",
            $"rotation={rotationLabel}",
            $"type={barcode.Type}",
            $"width={barcode.Width}, height={barcode.Height}",
            $"hri={barcode.Hri}",
            $"data=\"{EscapeDescriptionText(barcode.Data)}\"");
    }

    private static IReadOnlyList<string> BuildPrintGraphicDescription(PrintGraphic graphic)
    {
        return Lines(
            "GW x,y,width,height,[data] - Print graphic",
            $"x={graphic.X}, y={graphic.Y}",
            $"width={graphic.Width} (dots), height={graphic.Height} (dots)",
            $"dataLength={graphic.Data.Length} (bytes)");
    }

    private static IReadOnlyList<string> BuildDrawLineDescription(DrawBox drawBox)
    {
        return Lines(
            "X x1,y1,thickness,x2,y2 - Draw line or box",
            $"x1={drawBox.X1}, y1={drawBox.Y1}",
            $"thickness={drawBox.Thickness}",
            $"x2={drawBox.X2}, y2={drawBox.Y2}");
    }

    private static IReadOnlyList<string> BuildEplRasterImageDescription(EplRasterImage rasterImage)
    {
        return Lines(
            "GW x,y,width,height,[data] - Raster image (finalized)",
            $"x={rasterImage.X}, y={rasterImage.Y}",
            $"width={rasterImage.Width} (dots), height={rasterImage.Height} (dots)",
            $"contentType={rasterImage.Media.ContentType}",
            $"url={rasterImage.Media.Url}");
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

    private static string EscapeDescriptionText(string text)
    {
        return text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
