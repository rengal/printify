using Printify.Domain.Printing;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.Documents.EscPos;
using Printify.Infrastructure.Printing.EscPos.Commands;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Printify.Infrastructure.Mapping.EscPos;

/// <summary>
/// Converts between ESC/POS domain document elements and their serialized infrastructure representation.
/// </summary>
public static class EscPosDocumentElementMapper
{
    private const QrErrorCorrectionLevel DefaultQrCorrection = QrErrorCorrectionLevel.Medium;
    private const QrModel DefaultQrModel = QrModel.Model2;
    private const bool DefaultBoolean = false;

    public static EscPosDocumentElementPayload ToCommandPayload(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command switch
        {
            Bell => new BellElementPayload(),
            ParseError error => new ErrorElementPayload(error.Code, error.Message),
            CutPaper pagecut => new PagecutElementPayload(
                DomainMapper.ToString(pagecut.Mode),
                pagecut.FeedMotionUnits),
            PrinterError printerError => new PrinterErrorElementPayload(printerError.Message),
            GetPrinterStatus status => new PrinterStatusElementPayload(status.StatusByte, status.AdditionalStatusByte),
            PrintBarcode barcode => new PrintBarcodeElementPayload(
                DomainMapper.ToString(barcode.Symbology),
                barcode.Data,
                barcode.Width,
                barcode.Height,
                barcode.Media.Id),
            PrintQrCode qrCode => new PrintQrCodeElementPayload(
                qrCode.Data,
                qrCode.Width,
                qrCode.Height,
                qrCode.Media.Id),
            RasterImage image => new RasterImageElementPayload(image.Width, image.Height, image.Media.Id),
            Pulse pulse => new PulseElementPayload(pulse.Pin, pulse.OnTimeMs, pulse.OffTimeMs),
            Initialize => new ResetPrinterElementPayload(),
            SetBarcodeHeight height => new SetBarcodeHeightElementPayload(height.HeightInDots),
            SetBarcodeLabelPosition position => new SetBarcodeLabelPositionElementPayload(
                DomainMapper.ToString(position.Position)),
            SetBarcodeModuleWidth moduleWidth => new SetBarcodeModuleWidthElementPayload(moduleWidth.ModuleWidth),
            SetBoldMode bold => new SetBoldModeElementPayload(SerializeBool(bold.IsEnabled)),
            SetCodePage codePage => new SetCodePageElementPayload(codePage.CodePage),
            SelectFont font => new SetFontElementPayload(font.FontNumber, SerializeBool(font.IsDoubleWidth), SerializeBool(font.IsDoubleHeight)),
            SetJustification justification => new SetJustificationElementPayload(
                DomainMapper.ToString(justification.Justification)),
            SetLineSpacing spacing => new SetLineSpacingElementPayload(spacing.Spacing),
            ResetLineSpacing => new ResetLineSpacingElementPayload(),
            SetQrErrorCorrection correction => new SetQrErrorCorrectionElementPayload(
                DomainMapper.ToString(correction.Level)),
            SetQrModel model => new SetQrModelElementPayload(DomainMapper.ToString(model.Model)),
            SetQrModuleSize moduleSize => new SetQrModuleSizeElementPayload(moduleSize.ModuleSize),
            SetReverseMode reverse => new SetReverseModeElementPayload(SerializeBool(reverse.IsEnabled)),
            SetUnderlineMode underline => new SetUnderlineModeElementPayload(SerializeBool(underline.IsEnabled)),
            StoreQrData store => new StoreQrDataElementPayload(store.Content),
            StoredLogo logo => new StoredLogoElementPayload(logo.LogoId),
            AppendText append => new AppendToLineBufferElementPayload { RawBytesHex = Convert.ToHexString(append.TextBytes) },
            PrintAndLineFeed => new FlushLineBufferAndFeedElementPayload(),
            LegacyCarriageReturn => new LegacyCarriageReturnElementPayload(),
            StatusRequest request => new StatusRequestElementPayload((byte)request.RequestType),
            StatusResponse response => new StatusResponseElementPayload(
                response.StatusByte,
                response.IsPaperOut,
                response.IsCoverOpen,
                response.IsOffline),
            RasterImageUpload => throw new NotSupportedException("Raster image persistence is handled separately."),
            _ => throw new NotSupportedException($"Element type '{command.GetType().Name}' is not supported.")
        };
    }

    public static Command ToDomain(EscPosDocumentElementPayload dto, Domain.Media.Media? media = null)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return dto switch
        {
            BellElementPayload => new Bell(),
            ErrorElementPayload error => new ParseError(error.Code ?? string.Empty, error.Message ?? string.Empty),
            PagecutElementPayload pagecut => new CutPaper(
                DomainMapper.ParsePagecutMode(pagecut.Mode ?? "Full"),
                pagecut.FeedMotionUnits),
            PrinterErrorElementPayload printerError => new PrinterError(printerError.Message ?? string.Empty),
            PrinterStatusElementPayload status => new GetPrinterStatus(status.StatusByte, status.AdditionalStatusByte),
            PrintBarcodeElementPayload barcode => new PrintBarcode(
                DomainMapper.ParseBarcodeSymbology(barcode.Symbology ?? "Code128"),
                barcode.Data,
                barcode.Width,
                barcode.Height,
                media),
            PrintQrCodeElementPayload qrCode => new PrintQrCode(
                qrCode.Data,
                qrCode.Width,
                qrCode.Height,
                media),
            PulseElementPayload pulse => new Pulse(pulse.Pin, pulse.OnTimeMs, pulse.OffTimeMs),
            ResetPrinterElementPayload => new Initialize(),
            SetBarcodeHeightElementPayload height => new SetBarcodeHeight(height.HeightInDots),
            SetBarcodeLabelPositionElementPayload position => new SetBarcodeLabelPosition(
                DomainMapper.ParseBarcodeLabelPosition(position.Position ?? "Below")),
            SetBarcodeModuleWidthElementPayload moduleWidth => new SetBarcodeModuleWidth(moduleWidth.ModuleWidth),
            SetBoldModeElementPayload bold => new SetBoldMode(bold.IsEnabled ?? DefaultBoolean),
            SetCodePageElementPayload codePage => new SetCodePage(codePage.CodePage ?? "437"),
            SetFontElementPayload font => new SelectFont(
                font.FontNumber,
                font.IsDoubleWidth ?? DefaultBoolean,
                font.IsDoubleHeight ?? DefaultBoolean),
            SetJustificationElementPayload justification => new SetJustification(
                DomainMapper.ParseTextJustification(justification.Justification ?? "Left")),
            SetLineSpacingElementPayload spacing => new SetLineSpacing(spacing.Spacing),
            ResetLineSpacingElementPayload => new ResetLineSpacing(),
            SetQrErrorCorrectionElementPayload correction => new SetQrErrorCorrection(
                DomainMapper.ParseQrErrorCorrectionLevel(correction.Level ?? "Medium")),
            SetQrModelElementPayload model => new SetQrModel(DomainMapper.ParseQrModel(model.Model ?? "Model2")),
            SetQrModuleSizeElementPayload moduleSize => new SetQrModuleSize(moduleSize.ModuleSize),
            SetReverseModeElementPayload reverse => new SetReverseMode(reverse.IsEnabled ?? DefaultBoolean),
            SetUnderlineModeElementPayload underline => new SetUnderlineMode(underline.IsEnabled ?? DefaultBoolean),
            StoreQrDataElementPayload store => new StoreQrData(store.Content ?? string.Empty),
            StoredLogoElementPayload logo => new StoredLogo(logo.LogoId),
            AppendToLineBufferElementPayload textLine => new AppendText(
                string.IsNullOrEmpty(textLine.RawBytesHex) ? Array.Empty<byte>() : Convert.FromHexString(textLine.RawBytesHex)),
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
            EscPosDocumentElementTypeNames.Pulse => JsonSerializer.Deserialize<PulseElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.ResetPrinter => JsonSerializer.Deserialize<ResetPrinterElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetBarcodeHeight => JsonSerializer.Deserialize<SetBarcodeHeightElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetBarcodeLabelPosition => JsonSerializer.Deserialize<SetBarcodeLabelPositionElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetBarcodeModuleWidth => JsonSerializer.Deserialize<SetBarcodeModuleWidthElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetBoldMode => JsonSerializer.Deserialize<SetBoldModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetCodePage => JsonSerializer.Deserialize<SetCodePageElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetFont => JsonSerializer.Deserialize<SetFontElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetJustification => JsonSerializer.Deserialize<SetJustificationElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetLineSpacing => JsonSerializer.Deserialize<SetLineSpacingElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.ResetLineSpacing => JsonSerializer.Deserialize<ResetLineSpacingElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetQrErrorCorrection => JsonSerializer.Deserialize<SetQrErrorCorrectionElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetQrModel => JsonSerializer.Deserialize<SetQrModelElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetQrModuleSize => JsonSerializer.Deserialize<SetQrModuleSizeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetReverseMode => JsonSerializer.Deserialize<SetReverseModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.SetUnderlineMode => JsonSerializer.Deserialize<SetUnderlineModeElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.StoreQrData => JsonSerializer.Deserialize<StoreQrDataElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.StoredLogo => JsonSerializer.Deserialize<StoredLogoElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.AppendToLineBuffer => JsonSerializer.Deserialize<AppendToLineBufferElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.FlushLineBufferAndFeed => JsonSerializer.Deserialize<FlushLineBufferAndFeedElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.LegacyCarriageReturn => JsonSerializer.Deserialize<LegacyCarriageReturnElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.StatusRequest => JsonSerializer.Deserialize<StatusRequestElementPayload>(entity.Payload, SerializerOptions),
            EscPosDocumentElementTypeNames.StatusResponse => JsonSerializer.Deserialize<StatusResponseElementPayload>(entity.Payload, SerializerOptions),
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
            SetCodePageElementPayload => EscPosDocumentElementTypeNames.SetCodePage,
            SetFontElementPayload => EscPosDocumentElementTypeNames.SetFont,
            SetJustificationElementPayload => EscPosDocumentElementTypeNames.SetJustification,
            SetLineSpacingElementPayload => EscPosDocumentElementTypeNames.SetLineSpacing,
            ResetLineSpacingElementPayload => EscPosDocumentElementTypeNames.ResetLineSpacing,
            SetQrErrorCorrectionElementPayload => EscPosDocumentElementTypeNames.SetQrErrorCorrection,
            SetQrModelElementPayload => EscPosDocumentElementTypeNames.SetQrModel,
            SetQrModuleSizeElementPayload => EscPosDocumentElementTypeNames.SetQrModuleSize,
            SetReverseModeElementPayload => EscPosDocumentElementTypeNames.SetReverseMode,
            SetUnderlineModeElementPayload => EscPosDocumentElementTypeNames.SetUnderlineMode,
            StoreQrDataElementPayload => EscPosDocumentElementTypeNames.StoreQrData,
            StoredLogoElementPayload => EscPosDocumentElementTypeNames.StoredLogo,
            AppendToLineBufferElementPayload => EscPosDocumentElementTypeNames.AppendToLineBuffer,
            FlushLineBufferAndFeedElementPayload => EscPosDocumentElementTypeNames.FlushLineBufferAndFeed,
            LegacyCarriageReturnElementPayload => EscPosDocumentElementTypeNames.LegacyCarriageReturn,
            RasterImageElementPayload => EscPosDocumentElementTypeNames.RasterImage,
            StatusRequestElementPayload => EscPosDocumentElementTypeNames.StatusRequest,
            StatusResponseElementPayload => EscPosDocumentElementTypeNames.StatusResponse,
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }
}
