using System.Text.Json;
using System.Text.Json.Serialization;
using Printify.Domain.Printing;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.Documents.EscPos;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Mapping.Protocols.EscPos;

/// <summary>
/// Converts between ESC/POS domain document commands and their serialized infrastructure representation.
/// </summary>
public static class CommandMapper
{
    private const EscPosQrErrorCorrectionLevel DefaultQrCorrection = EscPosQrErrorCorrectionLevel.Medium;
    private const EscPosQrModel DefaultQrModel = EscPosQrModel.Model2;
    private const bool DefaultBoolean = false;

    public static EscPosDocumentElementPayload ToCommandPayload(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command switch
        {
            EscPosBell => new BellElementPayload(),
            EscPosParseError error => new ErrorElementPayload(error.Code, error.Message),
            EscPosCutPaper pagecut => new PagecutElementPayload(
                EnumMapper.ToString(pagecut.Mode),
                pagecut.FeedMotionUnits),
            EscPosPrinterError printerError => new PrinterErrorElementPayload(printerError.Message),
            EscPosGetPrinterStatus status => new PrinterStatusElementPayload(status.StatusByte, status.AdditionalStatusByte),
            EscPosPrintBarcode barcode => new PrintBarcodeElementPayload(
                EnumMapper.ToString(barcode.Symbology),
                barcode.Data,
                barcode.Width,
                barcode.Height,
                barcode.Media.Id),
            EscPosPrintQrCode qrCode => new PrintQrCodeElementPayload(
                qrCode.Data,
                qrCode.Width,
                qrCode.Height,
                qrCode.Media.Id),
            EscPosRasterImageGs7630 image => new RasterImageGs7630ElementPayload(
                image.Width,
                image.Height,
                image.Media.Id),
            EscPosRasterImageGs284C image => new RasterImageGs284CElementPayload(
                image.Width,
                image.Height,
                image.Media.Id),
            EscPosRasterImageGs384C image => new RasterImageGs384CElementPayload(
                image.Width,
                image.Height,
                image.Media.Id),
            EscPosPulse pulse => new PulseElementPayload(pulse.Pin, pulse.OnTimeMs, pulse.OffTimeMs),
            EscPosInitialize => new ResetPrinterElementPayload(),
            EscPosSetBarcodeHeight height => new SetBarcodeHeightElementPayload(height.HeightInDots),
            EscPosSetBarcodeLabelPosition position => new SetBarcodeLabelPositionElementPayload(
                EnumMapper.ToString(position.Position)),
            EscPosSetBarcodeModuleWidth moduleWidth => new SetBarcodeModuleWidthElementPayload(moduleWidth.ModuleWidth),
            EscPosSetBoldMode bold => new SetBoldModeElementPayload(SerializeBool(bold.IsEnabled)),
            EscPosCancelBoldMode => new CancelBoldModeElementPayload(),
            EscPosSetDoubleStrikeMode doubleStrike => new SetDoubleStrikeModeElementPayload(SerializeBool(doubleStrike.IsEnabled)),
            EscPosSetCodePage codePage => new SetCodePageElementPayload(codePage.CodePage),
            EscPosSetPrintMode font => new SetPrintModePayload(font.FontNumber, SerializeBool(font.IsDoubleWidth), SerializeBool(font.IsDoubleHeight)),
            EscPosSetCharacterSize size => new SetCharacterSizeElementPayload(size.WidthMultiplier, size.HeightMultiplier),
            EscPosSetRightCharacterSpacing spacing => new SetRightCharacterSpacingElementPayload(spacing.Spacing),
            EscPosSetUpsideDownMode upsideDown => new SetUpsideDownModeElementPayload(SerializeBool(upsideDown.IsEnabled)),
            EscPosSetJustification justification => new SetJustificationElementPayload(
                EnumMapper.ToString(justification.Justification)),
            EscPosSetLineSpacing spacing => new SetLineSpacingElementPayload(spacing.Spacing),
            EscPosResetLineSpacing => new ResetLineSpacingElementPayload(),
            EscPosSetQrErrorCorrection correction => new SetQrErrorCorrectionElementPayload(
                EnumMapper.ToString(correction.Level)),
            EscPosSetQrModel model => new SetQrModelElementPayload(EnumMapper.ToString(model.Model)),
            EscPosSetQrModuleSize moduleSize => new SetQrModuleSizeElementPayload(moduleSize.ModuleSize),
            EscPosSetReverseMode reverse => new SetReverseModeElementPayload(SerializeBool(reverse.IsEnabled)),
            EscPosEnableItalicMode => new EnableItalicModeElementPayload(),
            EscPosDisableItalicMode => new DisableItalicModeElementPayload(),
            EscPosSetUnderlineMode underline => new SetUnderlineModeElementPayload(SerializeBool(underline.IsEnabled)),
            EscPosStoreQrData store => new StoreQrDataElementPayload(store.Content),
            EscPosPrintLogo logo => new StoredLogoElementPayload(logo.LogoId),
            EscPosAppendText append => new AppendToLineBufferElementPayload { RawBytesHex = Convert.ToHexString(append.TextBytes) },
            EscPosPrintAndLineFeed => new FlushLineBufferAndFeedElementPayload(),
            EscPosLegacyCarriageReturn => new LegacyCarriageReturnElementPayload(),
            EscPosStatusRequest request => new StatusRequestElementPayload((byte)request.RequestType),
            EscPosStatusResponse response => new StatusResponseElementPayload(
                response.StatusByte,
                response.IsPaperOut,
                response.IsCoverOpen,
                response.IsOffline,
                (byte)response.RequestType),
            EscPosRasterImageUploadGs7630 => throw new NotSupportedException(
                "Raster image persistence is handled separately."),
            EscPosRasterImageStore => throw new NotSupportedException(
                "Graphics data persistence is handled separately."),
            EscPosRasterImagePrintUploadGs284C => throw new NotSupportedException(
                "Graphics data persistence is handled separately."),
            EscPosSetFont setFont => new SetFontElementPayload(setFont.FontNumber),
            EscPosPrintAndFeedLines feedLines => new PrintAndFeedLinesElementPayload(feedLines.Lines),
            EscPosPrintAndFeedDots feedDots => new PrintAndFeedDotsElementPayload(feedDots.Dots),
            EscPosHorizontalTab => new HorizontalTabElementPayload(),
            EscPosSetHorizontalTabStops tabStops => new SetHorizontalTabStopsElementPayload(tabStops.Columns),
            _ => throw new NotSupportedException($"Element type '{command.GetType().Name}' is not supported.")
        };
    }

    public static Command ToDomain(EscPosDocumentElementPayload dto, Domain.Media.Media? media = null)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return dto switch
        {
            BellElementPayload => new EscPosBell(),
            ErrorElementPayload error => new EscPosParseError(error.Code ?? string.Empty, error.Message ?? string.Empty),
            PagecutElementPayload pagecut => new EscPosCutPaper(
                EnumMapper.ParsePagecutMode(pagecut.Mode ?? "Full"),
                pagecut.FeedMotionUnits),
            PrinterErrorElementPayload printerError => new EscPosPrinterError(printerError.Message ?? string.Empty),
            PrinterStatusElementPayload status => new EscPosGetPrinterStatus(status.StatusByte, status.AdditionalStatusByte),
            PrintBarcodeElementPayload barcode => new EscPosPrintBarcode(
                EnumMapper.ParseBarcodeSymbology(barcode.Symbology ?? "Code128"),
                barcode.Data,
                barcode.Width,
                barcode.Height,
                media),
            PrintQrCodeElementPayload qrCode => new EscPosPrintQrCode(
                qrCode.Data,
                qrCode.Width,
                qrCode.Height,
                media),
            PulseElementPayload pulse => new EscPosPulse(pulse.Pin, pulse.OnTimeMs, pulse.OffTimeMs),
            ResetPrinterElementPayload => new EscPosInitialize(),
            SetBarcodeHeightElementPayload height => new EscPosSetBarcodeHeight(height.HeightInDots),
            SetBarcodeLabelPositionElementPayload position => new EscPosSetBarcodeLabelPosition(
                EnumMapper.ParseBarcodeLabelPosition(position.Position ?? "Below")),
            SetBarcodeModuleWidthElementPayload moduleWidth => new EscPosSetBarcodeModuleWidth(moduleWidth.ModuleWidth),
            SetBoldModeElementPayload bold => new EscPosSetBoldMode(bold.IsEnabled ?? DefaultBoolean),
            CancelBoldModeElementPayload => new EscPosCancelBoldMode(),
            SetDoubleStrikeModeElementPayload doubleStrike => new EscPosSetDoubleStrikeMode(doubleStrike.IsEnabled ?? DefaultBoolean),
            SetCodePageElementPayload codePage => new EscPosSetCodePage(codePage.CodePage ?? "437"),
            SetPrintModePayload font => new EscPosSetPrintMode(
                font.FontNumber,
                font.IsDoubleWidth ?? DefaultBoolean,
                font.IsDoubleHeight ?? DefaultBoolean),
            SetCharacterSizeElementPayload size => new EscPosSetCharacterSize(size.WidthMultiplier, size.HeightMultiplier),
            SetRightCharacterSpacingElementPayload spacing => new EscPosSetRightCharacterSpacing(spacing.Spacing),
            SetUpsideDownModeElementPayload upsideDown => new EscPosSetUpsideDownMode(upsideDown.IsEnabled ?? DefaultBoolean),
            SetJustificationElementPayload justification => new EscPosSetJustification(
                EnumMapper.ParseTextJustification(justification.Justification ?? "Left")),
            SetLineSpacingElementPayload spacing => new EscPosSetLineSpacing(spacing.Spacing),
            ResetLineSpacingElementPayload => new EscPosResetLineSpacing(),
            SetQrErrorCorrectionElementPayload correction => new EscPosSetQrErrorCorrection(
                EnumMapper.ParseQrErrorCorrectionLevel(correction.Level ?? "Medium")),
            SetQrModelElementPayload model => new EscPosSetQrModel(EnumMapper.ParseQrModel(model.Model ?? "Model2")),
            SetQrModuleSizeElementPayload moduleSize => new EscPosSetQrModuleSize(moduleSize.ModuleSize),
            SetReverseModeElementPayload reverse => new EscPosSetReverseMode(reverse.IsEnabled ?? DefaultBoolean),
            EnableItalicModeElementPayload => new EscPosEnableItalicMode(),
            DisableItalicModeElementPayload => new EscPosDisableItalicMode(),
            SetUnderlineModeElementPayload underline => new EscPosSetUnderlineMode(underline.IsEnabled ?? DefaultBoolean),
            StoreQrDataElementPayload store => new EscPosStoreQrData(store.Content ?? string.Empty),
            StoredLogoElementPayload logo => new EscPosPrintLogo(logo.LogoId),
            AppendToLineBufferElementPayload textLine => new EscPosAppendText(
                string.IsNullOrEmpty(textLine.RawBytesHex) ? Array.Empty<byte>() : Convert.FromHexString(textLine.RawBytesHex)),
            FlushLineBufferAndFeedElementPayload => new EscPosPrintAndLineFeed(),
            LegacyCarriageReturnElementPayload => new EscPosLegacyCarriageReturn(),
            StatusRequestElementPayload request => new EscPosStatusRequest((EscPosStatusRequestType)request.RequestType),
            StatusResponseElementPayload response => new EscPosStatusResponse(
                response.StatusByte,
                RequestType: (EscPosStatusRequestType)response.RequestType,
                response.IsPaperOut,
                response.IsCoverOpen,
                response.IsOffline),
            // Legacy rasterImage rows were created from GS v 0 before persisted raster variants existed.
            RasterImageElementPayload raster => new EscPosRasterImageGs7630(
                raster.Width,
                raster.Height,
                media),
            RasterImageGs7630ElementPayload raster => new EscPosRasterImageGs7630(
                raster.Width,
                raster.Height,
                media),
            RasterImageGs284CElementPayload raster => new EscPosRasterImageGs284C(
                raster.Width,
                raster.Height,
                media),
            RasterImageGs384CElementPayload raster => new EscPosRasterImageGs384C(
                raster.Width,
                raster.Height,
                media),
            SetFontElementPayload charFont => new EscPosSetFont(charFont.FontNumber),
            PrintAndFeedLinesElementPayload feedLines => new EscPosPrintAndFeedLines(feedLines.Lines),
            PrintAndFeedDotsElementPayload feedDots => new EscPosPrintAndFeedDots(feedDots.Dots),
            HorizontalTabElementPayload => new EscPosHorizontalTab(),
            SetHorizontalTabStopsElementPayload tabStops => new EscPosSetHorizontalTabStops(tabStops.Columns),
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }

    internal static DocumentElementEntity ToEntity(
        Guid documentId,
        EscPosDocumentElementPayload dto,
        int sequence,
        byte[] rawBytes,
        int lengthInBytes)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(rawBytes);

        return new DocumentElementEntity
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Sequence = sequence,
            ElementType = ResolveElementType(dto),
            Payload = JsonSerializer.Serialize(dto, dto.GetType(), SerializerOptions),
            CommandRaw = rawBytes.Length == 0 ? string.Empty : Convert.ToHexString(rawBytes),
            LengthInBytes = lengthInBytes
        };
    }

    internal static EscPosDocumentElementPayload? ToDto(DocumentElementEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (string.IsNullOrWhiteSpace(entity.Payload))
        {
            return null;
        }

        return entity.ElementType switch
        {
            EscPosDocumentElementTypeNames.Bell => JsonSerializer.Deserialize<BellElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.Error => JsonSerializer.Deserialize<ErrorElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.Pagecut => JsonSerializer.Deserialize<PagecutElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.PrinterError => JsonSerializer.Deserialize<PrinterErrorElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.PrinterStatus => JsonSerializer.Deserialize<PrinterStatusElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.PrintBarcode => JsonSerializer.Deserialize<PrintBarcodeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.PrintQrCode => JsonSerializer.Deserialize<PrintQrCodeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.RasterImage => JsonSerializer.Deserialize<RasterImageElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.RasterImageGs7630 => JsonSerializer.Deserialize<RasterImageGs7630ElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.RasterImageGs284C => JsonSerializer.Deserialize<RasterImageGs284CElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.RasterImageGs384C => JsonSerializer.Deserialize<RasterImageGs384CElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.Pulse => JsonSerializer.Deserialize<PulseElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.ResetPrinter => JsonSerializer.Deserialize<ResetPrinterElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetBarcodeHeight => JsonSerializer.Deserialize<SetBarcodeHeightElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetBarcodeLabelPosition => JsonSerializer.Deserialize<SetBarcodeLabelPositionElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetBarcodeModuleWidth => JsonSerializer.Deserialize<SetBarcodeModuleWidthElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetBoldMode => JsonSerializer.Deserialize<SetBoldModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.CancelBoldMode => JsonSerializer.Deserialize<CancelBoldModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetDoubleStrikeMode => JsonSerializer.Deserialize<SetDoubleStrikeModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetCodePage => JsonSerializer.Deserialize<SetCodePageElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetPrintMode => JsonSerializer.Deserialize<SetPrintModePayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetCharacterSize => JsonSerializer.Deserialize<SetCharacterSizeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetRightCharacterSpacing => JsonSerializer.Deserialize<SetRightCharacterSpacingElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetUpsideDownMode => JsonSerializer.Deserialize<SetUpsideDownModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetJustification => JsonSerializer.Deserialize<SetJustificationElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetLineSpacing => JsonSerializer.Deserialize<SetLineSpacingElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.ResetLineSpacing => JsonSerializer.Deserialize<ResetLineSpacingElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetQrErrorCorrection => JsonSerializer.Deserialize<SetQrErrorCorrectionElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetQrModel => JsonSerializer.Deserialize<SetQrModelElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetQrModuleSize => JsonSerializer.Deserialize<SetQrModuleSizeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetReverseMode => JsonSerializer.Deserialize<SetReverseModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.EnableItalicMode => JsonSerializer.Deserialize<EnableItalicModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.DisableItalicMode => JsonSerializer.Deserialize<DisableItalicModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetUnderlineMode => JsonSerializer.Deserialize<SetUnderlineModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.StoreQrData => JsonSerializer.Deserialize<StoreQrDataElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.StoredLogo => JsonSerializer.Deserialize<StoredLogoElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.AppendToLineBuffer => JsonSerializer.Deserialize<AppendToLineBufferElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.FlushLineBufferAndFeed => JsonSerializer.Deserialize<FlushLineBufferAndFeedElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.LegacyCarriageReturn => JsonSerializer.Deserialize<LegacyCarriageReturnElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.StatusRequest => JsonSerializer.Deserialize<StatusRequestElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.StatusResponse => JsonSerializer.Deserialize<StatusResponseElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetFont => JsonSerializer.Deserialize<SetFontElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.PrintAndFeedLines => JsonSerializer.Deserialize<PrintAndFeedLinesElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.PrintAndFeedDots => JsonSerializer.Deserialize<PrintAndFeedDotsElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.HorizontalTab => JsonSerializer.Deserialize<HorizontalTabElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetHorizontalTabStops => JsonSerializer.Deserialize<SetHorizontalTabStopsElementPayload>(entity.Payload, SerializerOptions),
            _ => throw new NotSupportedException($"Element type '{entity.ElementType}' is not supported.")
        };
    }

    private static bool? SerializeBool(bool value)
    {
        return value ? true : null;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private static string ResolveElementType(EscPosDocumentElementPayload dto)
    {
        return dto switch
        {
            BellElementPayload => EscPosDocumentElementTypeNames.Bell,
            ErrorElementPayload => EscPosDocumentElementTypeNames.Error,
            PagecutElementPayload => EscPosDocumentElementTypeNames.Pagecut,
            PrinterErrorElementPayload => EscPosDocumentElementTypeNames.PrinterError,
            PrinterStatusElementPayload => EscPosDocumentElementTypeNames.PrinterStatus,
            PrintBarcodeElementPayload => EscPosDocumentElementTypeNames.PrintBarcode,
            PrintQrCodeElementPayload => EscPosDocumentElementTypeNames.PrintQrCode,
            PulseElementPayload => EscPosDocumentElementTypeNames.Pulse,
            ResetPrinterElementPayload => EscPosDocumentElementTypeNames.ResetPrinter,
            SetBarcodeHeightElementPayload => EscPosDocumentElementTypeNames.SetBarcodeHeight,
            SetBarcodeLabelPositionElementPayload => EscPosDocumentElementTypeNames.SetBarcodeLabelPosition,
            SetBarcodeModuleWidthElementPayload => EscPosDocumentElementTypeNames.SetBarcodeModuleWidth,
            SetBoldModeElementPayload => EscPosDocumentElementTypeNames.SetBoldMode,
            CancelBoldModeElementPayload => EscPosDocumentElementTypeNames.CancelBoldMode,
            SetDoubleStrikeModeElementPayload => EscPosDocumentElementTypeNames.SetDoubleStrikeMode,
            SetCodePageElementPayload => EscPosDocumentElementTypeNames.SetCodePage,
            SetPrintModePayload => EscPosDocumentElementTypeNames.SetPrintMode,
            SetCharacterSizeElementPayload => EscPosDocumentElementTypeNames.SetCharacterSize,
            SetRightCharacterSpacingElementPayload => EscPosDocumentElementTypeNames.SetRightCharacterSpacing,
            SetUpsideDownModeElementPayload => EscPosDocumentElementTypeNames.SetUpsideDownMode,
            SetJustificationElementPayload => EscPosDocumentElementTypeNames.SetJustification,
            SetLineSpacingElementPayload => EscPosDocumentElementTypeNames.SetLineSpacing,
            ResetLineSpacingElementPayload => EscPosDocumentElementTypeNames.ResetLineSpacing,
            SetQrErrorCorrectionElementPayload => EscPosDocumentElementTypeNames.SetQrErrorCorrection,
            SetQrModelElementPayload => EscPosDocumentElementTypeNames.SetQrModel,
            SetQrModuleSizeElementPayload => EscPosDocumentElementTypeNames.SetQrModuleSize,
            SetReverseModeElementPayload => EscPosDocumentElementTypeNames.SetReverseMode,
            EnableItalicModeElementPayload => EscPosDocumentElementTypeNames.EnableItalicMode,
            DisableItalicModeElementPayload => EscPosDocumentElementTypeNames.DisableItalicMode,
            SetUnderlineModeElementPayload => EscPosDocumentElementTypeNames.SetUnderlineMode,
            StoreQrDataElementPayload => EscPosDocumentElementTypeNames.StoreQrData,
            StoredLogoElementPayload => EscPosDocumentElementTypeNames.StoredLogo,
            AppendToLineBufferElementPayload => EscPosDocumentElementTypeNames.AppendToLineBuffer,
            FlushLineBufferAndFeedElementPayload => EscPosDocumentElementTypeNames.FlushLineBufferAndFeed,
            LegacyCarriageReturnElementPayload => EscPosDocumentElementTypeNames.LegacyCarriageReturn,
            RasterImageElementPayload => EscPosDocumentElementTypeNames.RasterImage,
            RasterImageGs7630ElementPayload => EscPosDocumentElementTypeNames.RasterImageGs7630,
            RasterImageGs284CElementPayload => EscPosDocumentElementTypeNames.RasterImageGs284C,
            RasterImageGs384CElementPayload => EscPosDocumentElementTypeNames.RasterImageGs384C,
            StatusRequestElementPayload => EscPosDocumentElementTypeNames.StatusRequest,
            StatusResponseElementPayload => EscPosDocumentElementTypeNames.StatusResponse,
            SetFontElementPayload => EscPosDocumentElementTypeNames.SetFont,
            PrintAndFeedLinesElementPayload => EscPosDocumentElementTypeNames.PrintAndFeedLines,
            PrintAndFeedDotsElementPayload => EscPosDocumentElementTypeNames.PrintAndFeedDots,
            HorizontalTabElementPayload => EscPosDocumentElementTypeNames.HorizontalTab,
            SetHorizontalTabStopsElementPayload => EscPosDocumentElementTypeNames.SetHorizontalTabStops,
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }
}
