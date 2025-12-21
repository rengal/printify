using System;
using System.Collections.Generic;
using System.Linq;
using Printify.Domain.Documents;
using Printify.Domain.Mapping;
using Printify.Domain.Media;
using Printify.Web.Contracts.Documents.Responses;
using DomainElements = Printify.Domain.Documents.Elements;
using ResponseElements = Printify.Web.Contracts.Documents.Responses.Elements;

namespace Printify.Web.Mapping;

public static class DocumentMapper
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

    internal static DocumentDto ToResponseDto(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var responseElements = document.Elements?
            .Select(ToResponseElement)
            .ToList()
            ?? new List<ResponseElements.ResponseElementDto>();

        return new DocumentDto(
            document.Id,
            document.PrintJobId,
            document.PrinterId,
            document.CreatedAt,
            DomainMapper.ToString(document.Protocol),
            document.WidthInDots,
            document.HeightInDots,
            document.ClientAddress,
            responseElements.AsReadOnly());
    }

    public static ResponseElements.ResponseElementDto ToResponseElement(DomainElements.Element element)
    {
        return element switch
        {
            DomainElements.Bell bell => WithCommandMetadata(new ResponseElements.Bell(), bell),
            DomainElements.Error error => WithCommandMetadata(
                new ResponseElements.ErrorDto(error.Code, error.Message),
                error),
            DomainElements.Pagecut pagecut => WithCommandMetadata(new ResponseElements.PagecutDto(), pagecut),
            DomainElements.PrinterError printerError => WithCommandMetadata(
                new ResponseElements.ErrorDto(string.Empty, printerError.Message),
                printerError),
            DomainElements.GetPrinterStatus status => WithCommandMetadata(
                new ResponseElements.PrinterStatusDto(
                    status.StatusByte,
                    status.AdditionalStatusByte.HasValue ? $"0x{status.AdditionalStatusByte.Value:X2}" : null),
                status),
            DomainElements.PrintBarcode barcode => WithCommandMetadata(
                new ResponseElements.PrintBarcodeDto(
                    DomainMapper.ToString(barcode.Symbology),
                    barcode.Width,
                    barcode.Height,
                    ToMediaDto(barcode.Media)),
                barcode),
            DomainElements.PrintQrCode qr => WithCommandMetadata(
                new ResponseElements.PrintQrCodeDto(
                    qr.Data,
                    qr.Width,
                    qr.Height,
                    ToMediaDto(qr.Media)),
                qr),
            DomainElements.Pulse pulse => WithCommandMetadata(
                new ResponseElements.PulseDto(
                    pulse.Pin,
                    pulse.OnTimeMs,
                    pulse.OffTimeMs),
                pulse),
            DomainElements.RasterImage raster => WithCommandMetadata(
                new ResponseElements.RasterImageDto(
                    raster.Width,
                    raster.Height,
                    ToMediaDto(raster.Media)),
                raster),
            DomainElements.ResetPrinter resetPrinter => WithCommandMetadata(
                new ResponseElements.ResetPrinterDto(),
                resetPrinter),
            DomainElements.SetBarcodeHeight height => WithCommandMetadata(
                new ResponseElements.SetBarcodeHeightDto(height.HeightInDots),
                height),
            DomainElements.SetBarcodeLabelPosition position => WithCommandMetadata(
                new ResponseElements.SetBarcodeLabelPositionDto(DomainMapper.ToString(position.Position)),
                position),
            DomainElements.SetBarcodeModuleWidth moduleWidth => WithCommandMetadata(
                new ResponseElements.SetBarcodeModuleWidthDto(moduleWidth.ModuleWidth),
                moduleWidth),
            DomainElements.SetBoldMode bold => WithCommandMetadata(
                new ResponseElements.SetBoldModeDto(bold.IsEnabled),
                bold),
            DomainElements.SetCodePage codePage => WithCommandMetadata(
                new ResponseElements.SetCodePageDto(codePage.CodePage),
                codePage),
            DomainElements.SetFont font => WithCommandMetadata(
                new ResponseElements.SetFontDto(font.FontNumber, font.IsDoubleWidth, font.IsDoubleHeight),
                font),
            DomainElements.SetJustification justification => WithCommandMetadata(
                new ResponseElements.SetJustificationDto(DomainMapper.ToString(justification.Justification)),
                justification),
            DomainElements.SetLineSpacing spacing => WithCommandMetadata(
                new ResponseElements.SetLineSpacingDto(spacing.Spacing),
                spacing),
            DomainElements.ResetLineSpacing resetLineSpacing => WithCommandMetadata(
                new ResponseElements.ResetLineSpacingDto(),
                resetLineSpacing),
            DomainElements.SetQrErrorCorrection correction => WithCommandMetadata(
                new ResponseElements.SetQrErrorCorrectionDto(DomainMapper.ToString(correction.Level)),
                correction),
            DomainElements.SetQrModel model => WithCommandMetadata(
                new ResponseElements.SetQrModelDto(DomainMapper.ToString(model.Model)),
                model),
            DomainElements.SetQrModuleSize moduleSize => WithCommandMetadata(
                new ResponseElements.SetQrModuleSizeDto(moduleSize.ModuleSize),
                moduleSize),
            DomainElements.SetReverseMode reverse => WithCommandMetadata(
                new ResponseElements.SetReverseModeDto(reverse.IsEnabled),
                reverse),
            DomainElements.SetUnderlineMode underline => WithCommandMetadata(
                new ResponseElements.SetUnderlineModeDto(underline.IsEnabled),
                underline),
            DomainElements.StoreQrData store => WithCommandMetadata(
                new ResponseElements.StoreQrDataDto(store.Content),
                store),
            DomainElements.StoredLogo storedLogo => WithCommandMetadata(
                new ResponseElements.StoredLogoDto(storedLogo.LogoId),
                storedLogo),
            DomainElements.AppendToLineBuffer textLine => WithCommandMetadata(
                new ResponseElements.AppendToLineBufferDto(textLine.Text),
                textLine),
            DomainElements.FlushLineBufferAndFeed flushLine => WithCommandMetadata(
                new ResponseElements.FlushLineBufferAndFeedDto(),
                flushLine),
            _ => throw new NotSupportedException(
                $"Element type '{element.GetType().FullName}' is not supported in responses.")
        };
    }

    private static ResponseElements.MediaDto ToMediaDto(Media media)
    {
        ArgumentNullException.ThrowIfNull(media);

        return new ResponseElements.MediaDto(
            media.ContentType,
            media.Length,
            media.Sha256Checksum,
            media.Url);
    }

    private static T WithCommandMetadata<T>(T dto, DomainElements.Element element)
        where T : ResponseElements.ResponseElementDto
    {
        return dto with
        {
            CommandRaw = element.CommandRaw,
            CommandDescription = BuildCommandDescription(element)
        };
    }

    private static IReadOnlyList<string> BuildCommandDescription(DomainElements.Element element)
    {
        // Keep command descriptions short and stable for UI/debug consumers.
        return element switch
        {
            DomainElements.Bell => Lines(
                "BEL - Buzzer (beeper)"),
            DomainElements.Error error => Lines(
                "Tokenizer error (non-ESC/POS)",
                $"Code={error.Code}",
                $"Message=\"{EscapeDescriptionText(error.Message)}\""),
            DomainElements.PrinterError printerError => Lines(
                "Parser error (non-ESC/POS)",
                $"Message=\"{EscapeDescriptionText(printerError.Message)}\""),
            DomainElements.GetPrinterStatus status => BuildPrinterStatusDescription(status),
            DomainElements.Pulse pulse => Lines(
                "ESC p m t1 t2 - Cash drawer pulse",
                $"m={pulse.Pin}",
                $"t1={pulse.OnTimeMs}, t2={pulse.OffTimeMs}"),
            DomainElements.ResetPrinter => Lines(
                "ESC @ - Initialize printer"),
            DomainElements.SetBarcodeHeight height => Lines(
                "GS h n - Set barcode height",
                $"n={height.HeightInDots} (dots)"),
            DomainElements.SetBarcodeLabelPosition position => BuildBarcodeLabelPositionDescription(position),
            DomainElements.SetBarcodeModuleWidth moduleWidth => Lines(
                "GS w n - Set barcode module width",
                $"n={moduleWidth.ModuleWidth} (module width)"),
            DomainElements.SetBoldMode bold => Lines(
                "ESC E n - Turn emphasized (bold) mode on/off",
                $"n={(bold.IsEnabled ? 1 : 0)} ({(bold.IsEnabled ? "on" : "off")})"),
            DomainElements.SetCodePage codePage => BuildCodePageDescription(codePage.CodePage),
            DomainElements.SetFont font => BuildFontDescription(font),
            DomainElements.SetJustification justification => BuildJustificationDescription(justification.Justification),
            DomainElements.SetLineSpacing spacing => Lines(
                "ESC 3 n - Set line spacing",
                $"n={spacing.Spacing} (dots)"),
            DomainElements.ResetLineSpacing => Lines(
                "ESC 2 - Select default line spacing"),
            DomainElements.SetQrErrorCorrection correction => Lines(
                "GS ( k - QR Code: Select error correction level",
                $"fn=0x45, level={DomainMapper.ToString(correction.Level)}"),
            DomainElements.SetQrModel model => Lines(
                "GS ( k - QR Code: Select model",
                $"fn=0x41, model={DomainMapper.ToString(model.Model)}"),
            DomainElements.SetQrModuleSize moduleSize => Lines(
                "GS ( k - QR Code: Set module size",
                $"fn=0x43, size={moduleSize.ModuleSize} (dots)"),
            DomainElements.SetReverseMode reverse => Lines(
                "GS B n - Turn white/black reverse print mode on/off",
                $"n={(reverse.IsEnabled ? 1 : 0)} ({(reverse.IsEnabled ? "on" : "off")})"),
            DomainElements.SetUnderlineMode underline => Lines(
                "ESC - n - Turn underline mode on/off",
                $"n={(underline.IsEnabled ? 1 : 0)} ({(underline.IsEnabled ? "on" : "off")})"),
            DomainElements.StoreQrData store => Lines(
                "GS ( k - QR Code: Store data in the symbol storage area",
                "fn=0x50",
                $"DataLength={store.Content.Length}",
                $"Data=\"{EscapeDescriptionText(store.Content)}\""),
            DomainElements.PrintQrCodeUpload => Lines(
                "GS ( k - QR Code: Print the symbol data in the symbol storage area",
                "fn=0x51"),
            DomainElements.PrintQrCode qr => Lines(
                "GS ( k - QR Code: Print the symbol data in the symbol storage area",
                "fn=0x51",
                $"DataLength={qr.Data.Length}",
                $"Data=\"{EscapeDescriptionText(qr.Data)}\""),
            DomainElements.PrintBarcodeUpload barcodeUpload => BuildBarcodeDescription(
                barcodeUpload.Symbology,
                barcodeUpload.Data),
            DomainElements.PrintBarcode barcode => BuildBarcodeDescription(
                barcode.Symbology,
                barcode.Data),
            DomainElements.StoredLogo storedLogo => Lines(
                "FS p m n - Print stored logo",
                $"n={storedLogo.LogoId}"),
            DomainElements.AppendToLineBuffer textLine => Lines(
                "0x20-0xFF (excl. 0x7F) - Append to line buffer",
                $"len={textLine.Text.Length}",
                $"preview=\"{EscapeDescriptionText(textLine.Text)}\""),
            DomainElements.FlushLineBufferAndFeed => Lines(
                "LF - Flush line buffer and feed one line"),
            DomainElements.RasterImage raster => BuildRasterImageDescription(raster.Width, raster.Height),
            DomainElements.RasterImageUpload upload => BuildRasterImageDescription(upload.Width, upload.Height),
            DomainElements.Pagecut pagecut => BuildPagecutDescription(pagecut),
            _ => Lines("Unknown command")
        };
    }

    private static IReadOnlyList<string> BuildPrinterStatusDescription(DomainElements.GetPrinterStatus status)
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

    private static IReadOnlyList<string> BuildJustificationDescription(DomainElements.TextJustification justification)
    {
        var value = justification switch
        {
            DomainElements.TextJustification.Left => 0,
            DomainElements.TextJustification.Center => 1,
            DomainElements.TextJustification.Right => 2,
            _ => 0
        };

        return Lines(
            "ESC a n - Select justification",
            $"n={FormatHexByte((byte)value)} ({value}) - {DomainMapper.ToString(justification)}");
    }

    private static IReadOnlyList<string> BuildFontDescription(DomainElements.SetFont font)
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
        DomainElements.SetBarcodeLabelPosition position)
    {
        var value = position.Position switch
        {
            DomainElements.BarcodeLabelPosition.NotPrinted => 0,
            DomainElements.BarcodeLabelPosition.Above => 1,
            DomainElements.BarcodeLabelPosition.Below => 2,
            DomainElements.BarcodeLabelPosition.AboveAndBelow => 3,
            _ => 0
        };

        return Lines(
            "GS H n - Select HRI character print position",
            $"n={FormatHexByte((byte)value)} ({value}) - {DomainMapper.ToString(position.Position)}");
    }

    private static IReadOnlyList<string> BuildBarcodeDescription(
        DomainElements.BarcodeSymbology symbology,
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

    private static IReadOnlyList<string> BuildPagecutDescription(DomainElements.Pagecut pagecut)
    {
        // ESC i / ESC m are legacy partial cuts; GS V handles full/partial and optional feed.
        return pagecut.Mode switch
        {
            DomainElements.PagecutMode.PartialOnePoint => Lines(
                "ESC i - Partial cut (one point left uncut)"),
            DomainElements.PagecutMode.PartialThreePoint => Lines(
                "ESC m - Partial cut (three points left uncut)"),
            _ => BuildGsPagecutDescription(pagecut)
        };
    }

    private static IReadOnlyList<string> BuildGsPagecutDescription(DomainElements.Pagecut pagecut)
    {
        var modeLabel = pagecut.Mode switch
        {
            DomainElements.PagecutMode.Full => "full",
            DomainElements.PagecutMode.Partial => "partial",
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
