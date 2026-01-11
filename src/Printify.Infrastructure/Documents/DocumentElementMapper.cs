namespace Printify.Infrastructure.Documents;

using System.Collections.Generic;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.EscPos;
using Printify.Domain.Media;

/// <summary>
/// Converts between domain document elements and their serialized infrastructure representation.
/// </summary>
public static class DocumentElementMapper
{
    private const QrErrorCorrectionLevel DefaultQrCorrection = QrErrorCorrectionLevel.Medium;
    private const QrModel DefaultQrModel = QrModel.Model2;
    private const bool DefaultBoolean = false;

    public static DocumentElementPayload ToDto(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element switch
        {
            Bell => new BellElementPayload(),
            ParseError error => new ErrorElementPayload(error.Code, error.Message),
            CutPaper pagecut => new PagecutElementPayload(pagecut.Mode.ToString(), pagecut.FeedMotionUnits),
            PrinterError printerError => new PrinterErrorElementPayload(printerError.Message),
            GetPrinterStatus status => new PrinterStatusElementPayload(status.StatusByte, status.AdditionalStatusByte),
            PrintBarcode barcode => new PrintBarcodeElementPayload(
                SerializeEnum(barcode.Symbology, BarcodeSymbology.Code39),
                barcode.Data,
                barcode.Width,
                barcode.Height,
                barcode.Media.Id),
            PrintQrCode qrCode => new PrintQrCodeElementPayload(
                qrCode.Data,
                qrCode.Width,
                qrCode.Height,
                qrCode.Media.Id),
            RasterImage image => new RasterImageElementPayload(image.Width,
                image.Height,
                image.Media.Id),
            Pulse pulse => new PulseElementPayload(pulse.Pin, pulse.OnTimeMs, pulse.OffTimeMs),
            Initialize => new ResetPrinterElementPayload(),
            SetBarcodeHeight height => new SetBarcodeHeightElementPayload(height.HeightInDots),
            SetBarcodeLabelPosition position => new SetBarcodeLabelPositionElementPayload(
                SerializeEnum(position.Position, Domain.Documents.Elements.BarcodeLabelPosition.Below)),
            SetBarcodeModuleWidth moduleWidth => new SetBarcodeModuleWidthElementPayload(moduleWidth.ModuleWidth),
            SetBoldMode bold => new SetBoldModeElementPayload(SerializeBool(bold.IsEnabled)),
            SetCodePage codePage => new SetCodePageElementPayload(codePage.CodePage),
            SelectFont font => new SetFontElementPayload(font.FontNumber, SerializeBool(font.IsDoubleWidth), SerializeBool(font.IsDoubleHeight)),
            SetJustification justification => new SetJustificationElementPayload(
                SerializeEnum(justification.Justification, TextJustification.Left)),
            SetLineSpacing spacing => new SetLineSpacingElementPayload(spacing.Spacing),
            ResetLineSpacing => new ResetLineSpacingElementPayload(),
            SetQrErrorCorrection correction => new SetQrErrorCorrectionElementPayload(
                SerializeEnum(correction.Level, DefaultQrCorrection)),
            SetQrModel model => new SetQrModelElementPayload(SerializeEnum(model.Model, DefaultQrModel)),
            SetQrModuleSize moduleSize => new SetQrModuleSizeElementPayload(moduleSize.ModuleSize),
            SetReverseMode reverse => new SetReverseModeElementPayload(SerializeBool(reverse.IsEnabled)),
            SetUnderlineMode underline => new SetUnderlineModeElementPayload(SerializeBool(underline.IsEnabled)),
            StoreQrData store => new StoreQrDataElementPayload(store.Content),
            StoredLogo logo => new StoredLogoElementPayload(logo.LogoId),
            AppendText append => new AppendToLineBufferElementPayload(append.Text),
            PrintAndLineFeed => new FlushLineBufferAndFeedElementPayload(),
            LegacyCarriageReturn => new LegacyCarriageReturnElementPayload(),
            StatusRequest request => new StatusRequestElementPayload((byte)request.RequestType),
            StatusResponse response => new StatusResponseElementPayload(
                response.StatusByte,
                response.IsPaperOut,
                response.IsCoverOpen,
                response.IsOffline),
            RasterImageUpload => throw new NotSupportedException("Raster image persistence is handled separately."),
            _ => throw new NotSupportedException($"Element type '{element.GetType().Name}' is not supported.")
        };
    }

    public static Element ToDomain(DocumentElementPayload dto, Media? media = null)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return dto switch
        {
            BellElementPayload => new Bell(),
            ErrorElementPayload error => new ParseError(error.Code ?? string.Empty, error.Message ?? string.Empty),
            PagecutElementPayload pagecut => new CutPaper(
                ParseEnumOrDefault(pagecut.Mode, PagecutMode.Full),
                pagecut.FeedMotionUnits),
            PrinterErrorElementPayload printerError => new PrinterError(printerError.Message ?? string.Empty),
            PrinterStatusElementPayload status => new GetPrinterStatus(status.StatusByte, status.AdditionalStatusByte),
            PrintBarcodeElementPayload barcode => new PrintBarcode(
                ParseEnumOrDefault(barcode.Symbology, BarcodeSymbology.Code128),
                barcode.Data,
                barcode.Width,
                barcode.Height,
                media),
                //media ?? throw new InvalidOperationException("Raster image metadata is missing.")),
            PrintQrCodeElementPayload qrCode => new PrintQrCode(
                qrCode.Data,
                qrCode.Width,
                qrCode.Height,
                media),
                //media ?? throw new InvalidOperationException("Raster image metadata is missing.")),
            PulseElementPayload pulse => new Pulse(pulse.Pin, pulse.OnTimeMs, pulse.OffTimeMs),
            ResetPrinterElementPayload => new Initialize(),
            SetBarcodeHeightElementPayload height => new SetBarcodeHeight(height.HeightInDots),
            SetBarcodeLabelPositionElementPayload position => new SetBarcodeLabelPosition(
                ParseEnumOrDefault(position.Position, Domain.Documents.Elements.BarcodeLabelPosition.Below)),
            SetBarcodeModuleWidthElementPayload moduleWidth => new SetBarcodeModuleWidth(moduleWidth.ModuleWidth),
            SetBoldModeElementPayload bold => new SetBoldMode(bold.IsEnabled ?? DefaultBoolean),
            SetCodePageElementPayload codePage => new SetCodePage(codePage.CodePage ?? "437"),
            SetFontElementPayload font => new SelectFont(
                font.FontNumber,
                font.IsDoubleWidth ?? DefaultBoolean,
                font.IsDoubleHeight ?? DefaultBoolean),
            SetJustificationElementPayload justification => new SetJustification(
                ParseEnumOrDefault(justification.Justification, TextJustification.Left)),
            SetLineSpacingElementPayload spacing => new SetLineSpacing(spacing.Spacing),
            ResetLineSpacingElementPayload => new ResetLineSpacing(),
            SetQrErrorCorrectionElementPayload correction => new SetQrErrorCorrection(
                ParseEnumOrDefault(correction.Level, DefaultQrCorrection)),
            SetQrModelElementPayload model => new SetQrModel(ParseEnumOrDefault(model.Model, DefaultQrModel)),
            SetQrModuleSizeElementPayload moduleSize => new SetQrModuleSize(moduleSize.ModuleSize),
            SetReverseModeElementPayload reverse => new SetReverseMode(reverse.IsEnabled ?? DefaultBoolean),
            SetUnderlineModeElementPayload underline => new SetUnderlineMode(underline.IsEnabled ?? DefaultBoolean),
            StoreQrDataElementPayload store => new StoreQrData(store.Content ?? string.Empty),
            StoredLogoElementPayload logo => new StoredLogo(logo.LogoId),
            AppendToLineBufferElementPayload textLine => new AppendText(textLine.Text ?? string.Empty),
            FlushLineBufferAndFeedElementPayload => new PrintAndLineFeed(),
            LegacyCarriageReturnElementPayload => new LegacyCarriageReturn(),
            StatusRequestElementPayload request => new StatusRequest((StatusRequestType)request.RequestType),
            StatusResponseElementPayload response => new StatusResponse(
                response.StatusByte,
                response.IsPaperOut,
                response.IsCoverOpen,
                response.IsOffline),
            RasterImageElementPayload raster => new RasterImage(
                raster.Width,
                raster.Height,
                media),
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }

    private static bool? SerializeBool(bool value)
    {
        return value ? true : null;
    }

    private static string? SerializeEnum<T>(T value, T defaultValue) where T : struct, Enum
    {
        return EqualityComparer<T>.Default.Equals(value, defaultValue) ? null : value.ToString();
    }

    private static T ParseEnumOrDefault<T>(string? value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return Enum.TryParse<T>(value, ignoreCase: true, out var parsed)
            ? parsed
            : defaultValue;
    }
}

