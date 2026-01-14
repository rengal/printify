using System;
using System.Collections.Generic;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.EscPos;
using EplElements = Printify.Domain.Documents.Elements.Epl;
using Printify.Domain.Mapping;

namespace Printify.Application.Features.Printers.Documents;

public static class CommandDescriptionBuilder
{
    // ESC t n uses these ESC/POS code page IDs.
    private static readonly IReadOnlyDictionary<string, byte> CodePageIds =
        new Dictionary<string, byte>(StringComparer.Ordinal)
        {
            ["437"] = 0x00,
            ["720"] = 0x20,
            ["737"] = 0x0E,
            ["775"] = 0x21,
            ["850"] = 0x02,
            ["852"] = 0x12,
            ["855"] = 0x22,
            ["857"] = 0x0D,
            ["858"] = 0x13,
            ["860"] = 0x03,
            ["861"] = 0x23,
            ["862"] = 0x24,
            ["863"] = 0x04,
            ["864"] = 0x25,
            ["865"] = 0x05,
            ["866"] = 0x11,
            ["869"] = 0x26,
            ["1098"] = 0x29,
            ["1118"] = 0x2A,
            ["1119"] = 0x2B,
            ["1125"] = 0x2C,
            ["1250"] = 0x2D,
            ["1251"] = 0x2E,
            ["1252"] = 0x10,
            ["1253"] = 0x2F,
            ["1254"] = 0x30,
            ["1255"] = 0x31,
            ["1256"] = 0x32,
            ["1257"] = 0x33,
            ["1258"] = 0x34
        };

    private static readonly IReadOnlyDictionary<string, string> CodePageNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["866"] = "Cyrillic"
        };

    public static IReadOnlyList<string> Build(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);

        // Keep command descriptions short and stable for UI/debug consumers.
        return element switch
        {
            Bell => Lines(
                "BEL - Buzzer (beeper)"),
            ParseError error => Lines(
                "Parser error",
                $"Code={error.Code}",
                $"Message=\"{EscapeDescriptionText(error.Message)}\""),
            PrinterError printerError => Lines(
                "Printer error",
                $"Message=\"{EscapeDescriptionText(printerError.Message)}\""),
            GetPrinterStatus status => BuildPrinterStatusDescription(status),
            StatusRequest request => BuildStatusRequestDescription(request),
            StatusResponse response => BuildStatusResponseDescription(response),
            Pulse pulse => Lines(
                "ESC p m t1 t2 - Cash drawer pulse",
                $"m={pulse.Pin}",
                $"t1={pulse.OnTimeMs}, t2={pulse.OffTimeMs}"),
            Initialize => Lines(
                "ESC @ - Initialize printer"),
            SetBarcodeHeight height => Lines(
                "GS h n - Set barcode height",
                $"n={height.HeightInDots} (dots)"),
            SetBarcodeLabelPosition position => BuildBarcodeLabelPositionDescription(position),
            SetBarcodeModuleWidth moduleWidth => Lines(
                "GS w n - Set barcode module width",
                $"n={moduleWidth.ModuleWidth} (module width)"),
            SetBoldMode bold => Lines(
                "ESC E n - Turn emphasized (bold) mode on/off",
                $"n={(bold.IsEnabled ? 1 : 0)} ({(bold.IsEnabled ? "on" : "off")})"),
            SetCodePage codePage => BuildCodePageDescription(codePage.CodePage),
            SelectFont font => BuildFontDescription(font),
            SetJustification justification => BuildJustificationDescription(justification.Justification),
            SetLineSpacing spacing => Lines(
                "ESC 3 n - Set line spacing",
                $"n={spacing.Spacing} (dots)"),
            ResetLineSpacing => Lines(
                "ESC 2 - Select default line spacing"),
            SetQrErrorCorrection correction => Lines(
                "GS ( k - QR Code: Select error correction level",
                $"fn=0x45, level={DomainMapper.ToString(correction.Level)}"),
            SetQrModel model => Lines(
                "GS ( k - QR Code: Select model",
                $"fn=0x41, model={DomainMapper.ToString(model.Model)}"),
            SetQrModuleSize moduleSize => Lines(
                "GS ( k - QR Code: Set module size",
                $"fn=0x43, size={moduleSize.ModuleSize} (dots)"),
            SetReverseMode reverse => Lines(
                "GS B n - Turn white/black reverse print mode on/off",
                $"n={(reverse.IsEnabled ? 1 : 0)} ({(reverse.IsEnabled ? "on" : "off")})"),
            SetUnderlineMode underline => Lines(
                "ESC - n - Turn underline mode on/off",
                $"n={(underline.IsEnabled ? 1 : 0)} ({(underline.IsEnabled ? "on" : "off")})"),
            StoreQrData store => Lines(
                "GS ( k - QR Code: Store data in the symbol storage area",
                "fn=0x50",
                $"DataLength={store.Content.Length}",
                $"Data=\"{EscapeDescriptionText(store.Content)}\""),
            PrintQrCodeUpload => Lines(
                "GS ( k - QR Code: Print the symbol data in the symbol storage area",
                "fn=0x51"),
            PrintQrCode qr => Lines(
                "GS ( k - QR Code: Print the symbol data in the symbol storage area",
                "fn=0x51",
                $"DataLength={qr.Data.Length}",
                $"Data=\"{EscapeDescriptionText(qr.Data)}\""),
            PrintBarcodeUpload barcodeUpload => BuildBarcodeDescription(
                barcodeUpload.Symbology,
                barcodeUpload.Data),
            PrintBarcode barcode => BuildBarcodeDescription(
                barcode.Symbology,
                barcode.Data),
            StoredLogo storedLogo => Lines(
                "FS p m n - Print stored logo",
                $"n={storedLogo.LogoId}"),
            AppendText textLine => Lines(
                "0x20-0xFF (excl. 0x7F) - Append to line buffer",
                $"len={textLine.Text.Length}",
                $"preview=\"{EscapeDescriptionText(textLine.Text)}\""),
            PrintAndLineFeed => Lines(
                "LF - Flush line buffer and feed one line"),
            LegacyCarriageReturn => Lines(
                "CR - Carriage return (legacy compatibility)",
                "Ignored by the printer"),
            RasterImage raster => BuildRasterImageDescription(raster.Width, raster.Height),
            RasterImageUpload upload => BuildRasterImageDescription(upload.Width, upload.Height),
            CutPaper pagecut => BuildPagecutDescription(pagecut),
            // EPL Text Elements
            EplElements.ScalableText scalableText => BuildScalableTextDescription(scalableText),
            EplElements.DrawHorizontalLine horizontalLine => BuildDrawHorizontalLineDescription(horizontalLine),
            EplElements.Print print => BuildPrintDescription(print),
            // EPL Barcode Elements
            EplElements.PrintBarcode eplBarcode => BuildEplBarcodeDescription(eplBarcode),
            // EPL Graphics Elements
            EplElements.PrintGraphic graphic => BuildPrintGraphicDescription(graphic),
            // EPL Shape Elements
            EplElements.DrawLine drawLine => BuildDrawLineDescription(drawLine),
            // EPL Config Elements
            EplElements.ClearBuffer => Lines("N - Clear buffer (acknowledge/clear image buffer)"),
            EplElements.SetLabelWidth labelWidth => Lines(
                "q width - Set label width",
                $"width={labelWidth.Width} (dots)"),
            EplElements.SetLabelHeight labelHeight => Lines(
                "Q height, param2 - Set label height",
                $"height={labelHeight.Height} (dots)"),
            EplElements.SetPrintSpeed speed => Lines(
                "R speed - Set print speed",
                $"speed={speed.Speed} (ips)"),
            EplElements.SetPrintDarkness darkness => Lines(
                "S darkness - Set print darkness",
                $"darkness={darkness.Darkness}"),
            EplElements.SetPrintDirection direction => Lines(
                "Z direction - Set print direction",
                $"direction={direction.Direction}"),
            EplElements.SetInternationalCharacter intlChar => Lines(
                "I code - Set international character set",
                $"code={intlChar.Code}"),
            EplElements.SetCodePage eplCodePage => Lines(
                "i code, scaling - Set code page",
                $"code={eplCodePage.Code}, scaling={eplCodePage.Scaling}"),
            _ => Lines("Unknown command")
        };
    }

    private static IReadOnlyList<string> BuildPrinterStatusDescription(GetPrinterStatus status)
    {
        if (status.AdditionalStatusByte.HasValue)
        {
            return Lines(
                "DLE EOT n - Real-time status transmission",
                $"n={FormatHexByte(status.StatusByte)} ({status.StatusByte})",
                $"Status={FormatHexByte(status.AdditionalStatusByte.Value)}");
        }

        return Lines(
            "DLE EOT n - Real-time status transmission",
            $"n={FormatHexByte(status.StatusByte)} ({status.StatusByte})");
    }

    private static IReadOnlyList<string> BuildStatusRequestDescription(StatusRequest request)
    {
        var requestTypeDescription = request.RequestType switch
        {
            StatusRequestType.PrinterStatus => "printer status",
            StatusRequestType.OfflineCause => "offline cause",
            StatusRequestType.ErrorCause => "error cause",
            StatusRequestType.PaperRollSensor => "paper roll sensor",
            _ => "unknown"
        };

        return Lines(
            "DLE EOT n - Real-time status request",
            $"n={FormatHexByte((byte)request.RequestType)} ({(byte)request.RequestType})",
            $"Request type: {requestTypeDescription}");
    }

    private static IReadOnlyList<string> BuildStatusResponseDescription(StatusResponse response)
    {
        var flags = new List<string>();
        if (response.IsPaperOut)
            flags.Add("paper out");
        if (response.IsCoverOpen)
            flags.Add("cover open");
        if (response.IsOffline)
            flags.Add("offline");

        var flagsText = flags.Count > 0 ? string.Join(", ", flags) : "ready";

        return Lines(
            "Status response from printer",
            $"Status byte: {FormatHexByte(response.StatusByte)}",
            $"State: {flagsText}");
    }

    private static IReadOnlyList<string> BuildCodePageDescription(string codePage)
    {
        var nameSuffix = CodePageNames.TryGetValue(codePage, out var name)
            ? $" ({name})"
            : string.Empty;

        if (CodePageIds.TryGetValue(codePage, out var id))
        {
            return Lines(
                "ESC t n - Select character code table",
                $"n={FormatHexByte(id)} ({id}) - code page {codePage}{nameSuffix}");
        }

        return Lines(
            "ESC t n - Select character code table",
            $"code page {codePage}{nameSuffix}");
    }

    private static IReadOnlyList<string> BuildJustificationDescription(TextJustification justification)
    {
        var value = justification switch
        {
            TextJustification.Left => 0,
            TextJustification.Center => 1,
            TextJustification.Right => 2,
            _ => 0
        };

        return Lines(
            "ESC a n - Select justification",
            $"n={FormatHexByte((byte)value)} ({value}) - {DomainMapper.ToString(justification)}");
    }

    private static IReadOnlyList<string> BuildFontDescription(SelectFont font)
    {
        // ESC ! n: low 3 bits = font, bit 4 = double height, bit 5 = double width.
        var parameter = (byte)(font.FontNumber & 0x07);
        if (font.IsDoubleHeight)
        {
            parameter |= 0x10;
        }

        if (font.IsDoubleWidth)
        {
            parameter |= 0x20;
        }

        var fontLine = $"n={FormatHexByte(parameter)} (font={font.FontNumber}, dw={font.IsDoubleWidth}, " +
            $"dh={font.IsDoubleHeight})";

        return Lines(
            "ESC ! n - Select print mode",
            fontLine);
    }

    private static IReadOnlyList<string> BuildBarcodeLabelPositionDescription(
        SetBarcodeLabelPosition position)
    {
        var value = position.Position switch
        {
            BarcodeLabelPosition.NotPrinted => 0,
            BarcodeLabelPosition.Above => 1,
            BarcodeLabelPosition.Below => 2,
            BarcodeLabelPosition.AboveAndBelow => 3,
            _ => 0
        };

        return Lines(
            "GS H n - Select HRI character print position",
            $"n={FormatHexByte((byte)value)} ({value}) - {DomainMapper.ToString(position.Position)}");
    }

    private static IReadOnlyList<string> BuildBarcodeDescription(
        BarcodeSymbology symbology,
        string data)
    {
        return Lines(
            "GS k m - Print barcode",
            $"m={DomainMapper.ToString(symbology)}",
            $"DataLength={data.Length}",
            $"Data=\"{EscapeDescriptionText(data)}\"");
    }

    private static IReadOnlyList<string> BuildRasterImageDescription(int widthInDots, int heightInDots)
    {
        return Lines(
            "GS v 0 m xL xH yL yH - Print raster bit image",
            $"Width={widthInDots} (dots)",
            $"Height={heightInDots} (dots)");
    }

    private static IReadOnlyList<string> BuildPagecutDescription(CutPaper pagecut)
    {
        // ESC i / ESC m are legacy partial cuts; GS V handles full/partial and optional feed.
        return pagecut.Mode switch
        {
            PagecutMode.PartialOnePoint => Lines(
                "ESC i - Partial cut (one point left uncut)"),
            PagecutMode.PartialThreePoint => Lines(
                "ESC m - Partial cut (three points left uncut)"),
            _ => BuildGsPagecutDescription(pagecut)
        };
    }

    private static IReadOnlyList<string> BuildGsPagecutDescription(CutPaper pagecut)
    {
        var modeLabel = pagecut.Mode switch
        {
            PagecutMode.Full => "full",
            PagecutMode.Partial => "partial",
            _ => "full"
        };

        if (pagecut.FeedMotionUnits.HasValue)
        {
            return Lines(
                "GS V m n - Cut paper and feed",
                $"mode={modeLabel}",
                $"n={pagecut.FeedMotionUnits.Value} (feed units)");
        }

        return Lines(
            "GS V m - Cut paper",
            $"mode={modeLabel}");
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

    private static string FormatHexByte(byte value)
    {
        return $"0x{value:X2}";
    }

    // EPL command description builders

    private static IReadOnlyList<string> BuildScalableTextDescription(EplElements.ScalableText scalableText)
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
            $"text=\"{EscapeDescriptionText(scalableText.Text)}\"");
    }

    private static IReadOnlyList<string> BuildDrawHorizontalLineDescription(EplElements.DrawHorizontalLine horizontalLine)
    {
        return Lines(
            "LO x,y,thickness,length - Draw horizontal line",
            $"x={horizontalLine.X}, y={horizontalLine.Y}",
            $"thickness={horizontalLine.Thickness}, length={horizontalLine.Length}");
    }

    private static IReadOnlyList<string> BuildPrintDescription(EplElements.Print print)
    {
        return Lines(
            "P n - Print format and feed label",
            $"n={print.Copies} (copies)");
    }

    private static IReadOnlyList<string> BuildEplBarcodeDescription(EplElements.PrintBarcode barcode)
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

    private static IReadOnlyList<string> BuildPrintGraphicDescription(EplElements.PrintGraphic graphic)
    {
        return Lines(
            "GW x,y,width,height,[data] - Print graphic",
            $"x={graphic.X}, y={graphic.Y}",
            $"width={graphic.Width} (dots), height={graphic.Height} (dots)",
            $"dataLength={graphic.Data.Length} (bytes)");
    }

    private static IReadOnlyList<string> BuildDrawLineDescription(EplElements.DrawLine drawLine)
    {
        return Lines(
            "X x1,y1,thickness,x2,y2 - Draw line or box",
            $"x1={drawLine.X1}, y1={drawLine.Y1}",
            $"thickness={drawLine.Thickness}",
            $"x2={drawLine.X2}, y2={drawLine.Y2}");
    }
}
