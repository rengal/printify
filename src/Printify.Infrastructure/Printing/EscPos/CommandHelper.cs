using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Printify.Domain.Printing;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing.EscPos;

public static class EscPosCommandHelper
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

    public static IReadOnlyList<string> GetDescription(Command command, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        // For EscPosCommand, delegate to the typed overload
        if (command is EscPosCommand escPosCommand)
        {
            return GetDescription(escPosCommand, culture);
        }

        return Lines(
            $"Unknown command ({command.GetType().Name})",
            $"Raw bytes length={command.RawBytes?.Length ?? 0}");
    }

    public static IReadOnlyList<string> GetDescription(EscPosCommand command, CultureInfo? culture = null)
    {
        return GetDescription(command, textEncoding: null, culture);
    }

    public static IReadOnlyList<string> GetDescription(
        EscPosCommand command,
        Encoding? textEncoding,
        CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        // TODO: Use culture parameter for future localization

        // Keep command descriptions short and stable for UI/debug consumers.
        return command switch
        {
            EscPosBell => Lines(
                "BEL - Buzzer (beeper)"),
            EscPosParseError error => Lines(
                "Parser error",
                $"Code={error.Code}",
                $"Message=\"{EscapeDescriptionText(error.Message)}\""),
            EscPosPrinterError printerError => Lines(
                "Printer error",
                $"Message=\"{EscapeDescriptionText(printerError.Message)}\""),
            EscPosGetPrinterStatus status => BuildPrinterStatusDescription(status),
            EscPosStatusRequest request => BuildStatusRequestDescription(request),
            EscPosStatusResponse response => BuildStatusResponseDescription(response),
            EscPosPulse pulse => Lines(
                "ESC p m t1 t2 - Cash drawer pulse",
                $"m={pulse.Pin}",
                $"t1={pulse.OnTimeMs}, t2={pulse.OffTimeMs}"),
            EscPosInitialize => Lines(
                "ESC @ - Initialize printer"),
            EscPosSetBarcodeHeight height => Lines(
                "GS h n - Set barcode height",
                $"n={height.HeightInDots} (dots)"),
            EscPosSetBarcodeLabelPosition position => BuildBarcodeLabelPositionDescription(position),
            EscPosSetBarcodeModuleWidth moduleWidth => Lines(
                "GS w n - Set barcode module width",
                $"n={moduleWidth.ModuleWidth} (module width)"),
            EscPosSetFont charFont => Lines(
                "ESC M n - Select character font",
                $"n={charFont.FontNumber} (Font {(charFont.FontNumber == 0 ? "A" : "B")})"),
            EscPosSetCharacterSize size => BuildCharacterSizeDescription(size),
            EscPosSetRightCharacterSpacing spacing => Lines(
                "ESC SP n - Set right-side character spacing",
                $"n={spacing.Spacing} (dots)"),
            EscPosPrintAndFeedLines feedLines => Lines(
                "ESC d n - Print and feed n lines",
                $"n={feedLines.Lines}"),
            EscPosPrintAndFeedDots feedDots => Lines(
                "ESC J n - Print and feed paper",
                $"n={feedDots.Dots} (dots)"),
            EscPosSetBoldMode bold => Lines(
                "ESC E n - Turn emphasized (bold) mode on/off",
                $"n={(bold.IsEnabled ? 1 : 0)} ({(bold.IsEnabled ? "on" : "off")})"),
            EscPosCancelBoldMode => Lines(
                "ESC F - Cancel emphasized (bold) mode"),
            EscPosSetDoubleStrikeMode doubleStrike => Lines(
                "ESC G n - Turn double-strike mode on/off",
                $"n={(doubleStrike.IsEnabled ? 1 : 0)} ({(doubleStrike.IsEnabled ? "on" : "off")})"),
            EscPosSetCodePage codePage => BuildCodePageDescription(codePage.CodePage),
            EscPosSetPrintMode font => BuildFontDescription(font),
            EscPosEnableItalicMode => Lines(
                "ESC 4 - Turn italic mode on"),
            EscPosDisableItalicMode => Lines(
                "ESC 5 - Turn italic mode off"),
            EscPosSetUpsideDownMode upsideDown => Lines(
                "ESC { n - Turn upside-down mode on/off",
                $"n={(upsideDown.IsEnabled ? 1 : 0)} ({(upsideDown.IsEnabled ? "on" : "off")})"),
            EscPosHorizontalTab => Lines(
                "HT - Move to next horizontal tab stop"),
            EscPosSetHorizontalTabStops tabStops => BuildHorizontalTabStopsDescription(tabStops),
            EscPosSetJustification justification => BuildJustificationDescription(justification.Justification),
            EscPosSetLineSpacing spacing => Lines(
                "ESC 3 n - Set line spacing",
                $"n={spacing.Spacing} (dots)"),
            EscPosResetLineSpacing => Lines(
                "ESC 2 - Select default line spacing"),
            EscPosSetQrErrorCorrection correction => Lines(
                "GS ( k - QR Code: Select error correction level",
                $"fn=0x45, level={EnumMapper.ToString(correction.Level)}"),
            EscPosSetQrModel model => Lines(
                "GS ( k - QR Code: Select model",
                $"fn=0x41, model={EnumMapper.ToString(model.Model)}"),
            EscPosSetQrModuleSize moduleSize => Lines(
                "GS ( k - QR Code: Set module size",
                $"fn=0x43, size={moduleSize.ModuleSize} (dots)"),
            EscPosSetReverseMode reverse => Lines(
                "GS B n - Turn white/black reverse print mode on/off",
                $"n={(reverse.IsEnabled ? 1 : 0)} ({(reverse.IsEnabled ? "on" : "off")})"),
            EscPosSetUnderlineMode underline => Lines(
                "ESC - n - Turn underline mode on/off",
                $"n={(underline.IsEnabled ? 1 : 0)} ({(underline.IsEnabled ? "on" : "off")})"),
            EscPosStoreQrData store => Lines(
                "GS ( k - QR Code: Store data in the symbol storage area",
                "fn=0x50",
                $"DataLength={store.Content.Length}",
                $"Data=\"{EscapeDescriptionText(store.Content)}\""),
            EscPosPrintQrCodeUpload => Lines(
                "GS ( k - QR Code: Print the symbol data in the symbol storage area",
                "fn=0x51"),
            EscPosRasterImageStore store => Lines(
                "GS ( L / GS 8 L - Store raster graphics data",
                $"Width={store.Width} (dots)",
                $"Height={store.Height} (dots)"),
            EscPosRasterImagePrintUploadGs284C => Lines(
                "GS ( L - Print stored raster graphics data",
                "fn=0x32"),
            EscPosPrintQrCode qr => Lines(
                "GS ( k - QR Code: Print the symbol data in the symbol storage area",
                "fn=0x51",
                $"DataLength={qr.Data.Length}",
                $"Data=\"{EscapeDescriptionText(qr.Data)}\""),
            EscPosPrintBarcodeUpload barcodeUpload => BuildBarcodeDescription(
                barcodeUpload,
                barcodeUpload.Symbology,
                barcodeUpload.Data),
            EscPosPrintBarcode barcode => BuildBarcodeDescription(
                barcode,
                barcode.Symbology,
                barcode.Data),
            EscPosPrintLogo storedLogo => Lines(
                "FS p m n - Print stored logo",
                $"n={storedLogo.LogoId}"),
            EscPosAppendText textLine => BuildAppendTextDescription(
                textLine,
                textEncoding ?? Encoding.GetEncoding(437)),
            EscPosPrintAndLineFeed => Lines(
                "LF - Flush line buffer and feed one line"),
            EscPosLegacyCarriageReturn => Lines(
                "CR - Carriage return (legacy compatibility)",
                "Ignored by the printer"),
            EscPosRasterImage raster => BuildRasterImageDescription(raster.Width, raster.Height),
            EscPosRasterImageUploadGs7630 upload => BuildRasterImageDescription(upload.Width, upload.Height),
            EscPosCutPaper pagecut => BuildPagecutDescription(pagecut),
            _ => Lines(
                $"Unknown command ({command.GetType().Name})",
                $"Raw bytes length={command.RawBytes?.Length ?? 0}")
        };
    }

    private static IReadOnlyList<string> BuildPrinterStatusDescription(EscPosGetPrinterStatus status)
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

    private static IReadOnlyList<string> BuildStatusRequestDescription(EscPosStatusRequest request)
    {
        var requestTypeDescription = request.RequestType switch
        {
            EscPosStatusRequestType.PrinterStatus => "printer status",
            EscPosStatusRequestType.OfflineCause => "offline cause",
            EscPosStatusRequestType.ErrorCause => "error cause",
            EscPosStatusRequestType.PaperRollSensor => "paper roll sensor",
            _ => "unknown"
        };

        return Lines(
            "DLE EOT n - Real-time status request",
            $"n={FormatHexByte((byte)request.RequestType)} ({(byte)request.RequestType})",
            $"Request type: {requestTypeDescription}");
    }

    private static IReadOnlyList<string> BuildStatusResponseDescription(EscPosStatusResponse response)
    {
        var b = response.StatusByte;
        var flags = new List<string>();

        switch (response.RequestType)
        {
            case EscPosStatusRequestType.PrinterStatus:
                // bit3: offline, bit5: error, bit6: paper out
                if ((b & 0x08) != 0) flags.Add("offline");
                if ((b & 0x20) != 0) flags.Add("error");
                if ((b & 0x40) != 0) flags.Add("paper out");
                break;
            case EscPosStatusRequestType.OfflineCause:
                // bit2: cover open, bit5: paper out, bit6: error/offline
                if ((b & 0x04) != 0) flags.Add("cover open");
                if ((b & 0x20) != 0) flags.Add("paper out");
                if ((b & 0x40) != 0) flags.Add("error / offline / busy");
                break;
            case EscPosStatusRequestType.ErrorCause:
                // bit4: auto-cutter error, bit5: unrecoverable error, bit6: auto-recoverable error
                if ((b & 0x10) != 0) flags.Add("auto-cutter error");
                if ((b & 0x20) != 0) flags.Add("unrecoverable error");
                if ((b & 0x40) != 0) flags.Add("auto-recoverable error");
                break;
            case EscPosStatusRequestType.PaperRollSensor:
                // bit2-3: near end, bit5-6: paper out
                if ((b & 0x0C) != 0) flags.Add("paper near end");
                if ((b & 0x60) != 0) flags.Add("paper out");
                break;
        }

        var requestTypeLabel = response.RequestType switch
        {
            EscPosStatusRequestType.PrinterStatus => "printer status",
            EscPosStatusRequestType.OfflineCause => "offline cause",
            EscPosStatusRequestType.ErrorCause => "error cause",
            EscPosStatusRequestType.PaperRollSensor => "paper roll sensor",
            _ => "unknown"
        };

        var flagsText = flags.Count > 0 ? string.Join(", ", flags) : "ready";

        return Lines(
            "Status response from printer",
            $"n={FormatHexByte((byte)response.RequestType)} ({requestTypeLabel})",
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

    private static IReadOnlyList<string> BuildJustificationDescription(EscPosTextJustification justification)
    {
        var value = justification switch
        {
            EscPosTextJustification.Left => 0,
            EscPosTextJustification.Center => 1,
            EscPosTextJustification.Right => 2,
            _ => 0
        };

        return Lines(
            "ESC a n - Select justification",
            $"n={FormatHexByte((byte)value)} ({value}) - {EnumMapper.ToString(justification)}");
    }

    private static IReadOnlyList<string> BuildCharacterSizeDescription(EscPosSetCharacterSize size)
    {
        var parameter = (byte)(((size.WidthMultiplier - 1) << 4) | ((size.HeightMultiplier - 1) & 0x0F));

        return Lines(
            "GS ! n - Select character size",
            $"n={FormatHexByte(parameter)} (width={size.WidthMultiplier}, height={size.HeightMultiplier})");
    }

    private static IReadOnlyList<string> BuildHorizontalTabStopsDescription(EscPosSetHorizontalTabStops tabStops)
    {
        var stops = tabStops.Columns.Length == 0
            ? "clear all"
            : string.Join(", ", tabStops.Columns);

        return Lines(
            "ESC D n1 ... nk NUL - Set horizontal tab positions",
            $"Columns={stops}");
    }

    private static IReadOnlyList<string> BuildFontDescription(EscPosSetPrintMode font)
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
        EscPosSetBarcodeLabelPosition position)
    {
        var value = position.Position switch
        {
            EscPosBarcodeLabelPosition.NotPrinted => 0,
            EscPosBarcodeLabelPosition.Above => 1,
            EscPosBarcodeLabelPosition.Below => 2,
            EscPosBarcodeLabelPosition.AboveAndBelow => 3,
            _ => 0
        };

        return Lines(
            "GS H n - Select HRI character print position",
            $"n={FormatHexByte((byte)value)} ({value}) - {EnumMapper.ToString(position.Position)}");
    }

    private static IReadOnlyList<string> BuildBarcodeDescription(
        EscPosCommand command,
        EscPosBarcodeSymbology symbology,
        string data)
    {
        var symbologyDescription = EnumMapper.ToString(symbology);
        if (IsLegacyCode128(command))
        {
            symbologyDescription += " (legacy)";
        }

        return Lines(
            "GS k m - Print barcode",
            $"m={symbologyDescription}",
            $"DataLength={data.Length}",
            $"Data=\"{EscapeDescriptionText(data)}\"");
    }

    private static bool IsLegacyCode128(EscPosCommand command)
    {
        // Legacy GS k used m=0x08 for Code128 before the newer Epson selector range.
        return command.RawBytes is [0x1D, 0x6B, 0x08, ..];
    }

    private static IReadOnlyList<string> BuildRasterImageDescription(int widthInDots, int heightInDots)
    {
        return Lines(
            "GS v 0 m xL xH yL yH - Print raster bit image",
            $"Width={widthInDots} (dots)",
            $"Height={heightInDots} (dots)");
    }

    private static IReadOnlyList<string> BuildAppendTextDescription(EscPosAppendText textLine, Encoding textEncoding)
    {
        return Lines(
            "0x20-0xFF (excl. 0x7F) - Append to line buffer",
            $"len={textLine.TextBytes.Length}",
            $"preview=\"{EscapeDescriptionText(textEncoding.GetString(textLine.TextBytes))}\"");
    }

    private static IReadOnlyList<string> BuildPagecutDescription(EscPosCutPaper pagecut)
    {
        // ESC i / ESC m are legacy partial cuts; GS V handles full/partial and optional feed.
        return pagecut.Mode switch
        {
            EscPosPagecutMode.PartialOnePoint => Lines(
                "ESC i - Partial cut (one point left uncut)"),
            EscPosPagecutMode.PartialThreePoint => Lines(
                "ESC m - Partial cut (three points left uncut)"),
            _ => BuildGsPagecutDescription(pagecut)
        };
    }

    private static IReadOnlyList<string> BuildGsPagecutDescription(EscPosCutPaper pagecut)
    {
        var modeLabel = pagecut.Mode switch
        {
            EscPosPagecutMode.Full => "full",
            EscPosPagecutMode.Partial => "partial",
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
}
