namespace Printify.Infrastructure.Documents;

using Printify.Domain.Documents.Elements;

/// <summary>
/// Converts between domain document elements and their serialized infrastructure representation.
/// </summary>
public static class DocumentElementMapper
{
    private const QrErrorCorrectionLevel DefaultQrCorrection = QrErrorCorrectionLevel.Medium;
    private const QrModel DefaultQrModel = QrModel.Model2;
    private const bool DefaultBoolean = false;

    public static DocumentElementDto ToDto(Element element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element switch
        {
            Bell => new BellElementDto(),
            Error error => new ErrorElementDto(error.Code, error.Message),
            Pagecut pagecut => new PagecutElementDto(pagecut.Mode.ToString(), pagecut.FeedMotionUnits),
            PrinterError printerError => new PrinterErrorElementDto(printerError.Message),
            GetPrinterStatus status => new PrinterStatusElementDto(status.StatusByte, status.AdditionalStatusByte),
            PrintBarcode barcode => new PrintBarcodeElementDto(
                SerializeEnum(barcode.Symbology, BarcodeSymbology.Code39),
                barcode.Data),
            PrintQrCode => new PrintQrCodeElementDto(),
            Pulse pulse => new PulseElementDto(
                SerializeEnum(pulse.Pin, Domain.Documents.Elements.PulsePin.Drawer1),
                pulse.OnTimeMs,
                pulse.OffTimeMs),
            ResetPrinter => new ResetPrinterElementDto(),
            SetBarcodeHeight height => new SetBarcodeHeightElementDto(height.HeightInDots),
            SetBarcodeLabelPosition position => new SetBarcodeLabelPositionElementDto(
                SerializeEnum(position.Position, Domain.Documents.Elements.BarcodeLabelPosition.Below)),
            SetBarcodeModuleWidth moduleWidth => new SetBarcodeModuleWidthElementDto(moduleWidth.ModuleWidth),
            SetBoldMode bold => new SetBoldModeElementDto(SerializeBool(bold.IsEnabled)),
            SetCodePage codePage => new SetCodePageElementDto(codePage.CodePage),
            SetFont font => new SetFontElementDto(font.FontNumber, SerializeBool(font.IsDoubleWidth), SerializeBool(font.IsDoubleHeight)),
            SetJustification justification => new SetJustificationElementDto(
                SerializeEnum(justification.Justification, TextJustification.Left)),
            SetLineSpacing spacing => new SetLineSpacingElementDto(spacing.Spacing),
            ResetLineSpacing => new ResetLineSpacingElementDto(),
            SetQrErrorCorrection correction => new SetQrErrorCorrectionElementDto(
                SerializeEnum(correction.Level, DefaultQrCorrection)),
            SetQrModel model => new SetQrModelElementDto(SerializeEnum(model.Model, DefaultQrModel)),
            SetQrModuleSize moduleSize => new SetQrModuleSizeElementDto(moduleSize.ModuleSize),
            SetReverseMode reverse => new SetReverseModeElementDto(SerializeBool(reverse.IsEnabled)),
            SetUnderlineMode underline => new SetUnderlineModeElementDto(SerializeBool(underline.IsEnabled)),
            StoreQrData store => new StoreQrDataElementDto(store.Content),
            StoredLogo logo => new StoredLogoElementDto(logo.LogoId),
            TextLine textLine => new TextLineElementDto(textLine.Text),
            RasterImageUpload => throw new NotSupportedException("Raster image persistence is handled separately."),
            _ => throw new NotSupportedException($"Element type '{element.GetType().Name}' is not supported.")
        };
    }

    public static Element ToDomain(DocumentElementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return dto switch
        {
            BellElementDto => new Bell(),
            ErrorElementDto error => new Error(error.Code ?? string.Empty, error.Message ?? string.Empty),
            PagecutElementDto pagecut => new Pagecut(
                ParseEnumOrDefault(pagecut.Mode, PagecutMode.Full),
                pagecut.FeedMotionUnits),
            PrinterErrorElementDto printerError => new PrinterError(printerError.Message ?? string.Empty),
            PrinterStatusElementDto status => new GetPrinterStatus(status.StatusByte, status.AdditionalStatusByte),
            PrintBarcodeElementDto barcode => new PrintBarcode(
                ParseEnumOrDefault(barcode.Symbology, BarcodeSymbology.Code39),
                barcode.Data ?? string.Empty),
            PrintQrCodeElementDto => new PrintQrCode(),
            PulseElementDto pulse => new Pulse(
                ParseEnumOrDefault(pulse.Pin, Domain.Documents.Elements.PulsePin.Drawer1),
                pulse.OnTimeMs,
                pulse.OffTimeMs),
            ResetPrinterElementDto => new ResetPrinter(),
            SetBarcodeHeightElementDto height => new SetBarcodeHeight(height.HeightInDots),
            SetBarcodeLabelPositionElementDto position => new SetBarcodeLabelPosition(
                ParseEnumOrDefault(position.Position, Domain.Documents.Elements.BarcodeLabelPosition.Below)),
            SetBarcodeModuleWidthElementDto moduleWidth => new SetBarcodeModuleWidth(moduleWidth.ModuleWidth),
            SetBoldModeElementDto bold => new SetBoldMode(bold.IsEnabled ?? DefaultBoolean),
            SetCodePageElementDto codePage => new SetCodePage(codePage.CodePage ?? "437"),
            SetFontElementDto font => new SetFont(
                font.FontNumber,
                font.IsDoubleWidth ?? DefaultBoolean,
                font.IsDoubleHeight ?? DefaultBoolean),
            SetJustificationElementDto justification => new SetJustification(
                ParseEnumOrDefault(justification.Justification, TextJustification.Left)),
            SetLineSpacingElementDto spacing => new SetLineSpacing(spacing.Spacing),
            ResetLineSpacingElementDto => new ResetLineSpacing(),
            SetQrErrorCorrectionElementDto correction => new SetQrErrorCorrection(
                ParseEnumOrDefault(correction.Level, DefaultQrCorrection)),
            SetQrModelElementDto model => new SetQrModel(ParseEnumOrDefault(model.Model, DefaultQrModel)),
            SetQrModuleSizeElementDto moduleSize => new SetQrModuleSize(moduleSize.ModuleSize),
            SetReverseModeElementDto reverse => new SetReverseMode(reverse.IsEnabled ?? DefaultBoolean),
            SetUnderlineModeElementDto underline => new SetUnderlineMode(underline.IsEnabled ?? DefaultBoolean),
            StoreQrDataElementDto store => new StoreQrData(store.Content ?? string.Empty),
            StoredLogoElementDto logo => new StoredLogo(logo.LogoId),
            TextLineElementDto textLine => new TextLine(textLine.Text ?? string.Empty),
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
