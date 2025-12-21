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
            DomainElements.TextLine textLine => WithCommandMetadata(
                new ResponseElements.TextLineDto(textLine.Text),
                textLine),
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

    private static string BuildCommandDescription(DomainElements.Element element)
    {
        // Keep command descriptions short and stable for UI/debug consumers.
        return element switch
        {
            DomainElements.Bell => "Bell",
            DomainElements.Error error => $"Error ({error.Code})",
            DomainElements.PrinterError => "Printer Error",
            DomainElements.GetPrinterStatus => "Get Printer Status",
            DomainElements.Pulse pulse =>
                $"Pulse (pin={pulse.Pin}, on={pulse.OnTimeMs}ms, off={pulse.OffTimeMs}ms)",
            DomainElements.ResetPrinter => "Reset Printer",
            DomainElements.SetBarcodeHeight height => $"Set Barcode Height (dots={height.HeightInDots})",
            DomainElements.SetBarcodeLabelPosition position =>
                $"Set Barcode Label Position ({DomainMapper.ToString(position.Position)})",
            DomainElements.SetBarcodeModuleWidth moduleWidth =>
                $"Set Barcode Module Width (dots={moduleWidth.ModuleWidth})",
            DomainElements.SetBoldMode bold => $"Set Bold Mode (enabled={bold.IsEnabled})",
            DomainElements.SetCodePage codePage => $"Set Code Page ({codePage.CodePage})",
            DomainElements.SetFont font =>
                $"Set Font (n={font.FontNumber}, dw={font.IsDoubleWidth}, dh={font.IsDoubleHeight})",
            DomainElements.SetJustification justification =>
                $"Set Justification ({DomainMapper.ToString(justification.Justification)})",
            DomainElements.SetLineSpacing spacing => $"Set Line Spacing (dots={spacing.Spacing})",
            DomainElements.ResetLineSpacing => "Reset Line Spacing",
            DomainElements.SetQrErrorCorrection correction =>
                $"Set QR Error Correction ({DomainMapper.ToString(correction.Level)})",
            DomainElements.SetQrModel model => $"Set QR Model ({DomainMapper.ToString(model.Model)})",
            DomainElements.SetQrModuleSize moduleSize => $"Set QR Module Size (dots={moduleSize.ModuleSize})",
            DomainElements.SetReverseMode reverse => $"Set Reverse Mode (enabled={reverse.IsEnabled})",
            DomainElements.SetUnderlineMode underline => $"Set Underline Mode (enabled={underline.IsEnabled})",
            DomainElements.StoreQrData => "Store QR Data",
            DomainElements.PrintQrCodeUpload => "Print QR Code",
            DomainElements.PrintQrCode => "Print QR Code",
            DomainElements.PrintBarcodeUpload => "Print Barcode",
            DomainElements.PrintBarcode => "Print Barcode",
            DomainElements.StoredLogo storedLogo => $"Stored Logo (id={storedLogo.LogoId})",
            DomainElements.TextLine textLine => $"Text Line (len={textLine.Text.Length})",
            DomainElements.RasterImage => "Raster Image",
            DomainElements.RasterImageUpload => "Raster Image Upload",
            DomainElements.Pagecut pagecut => BuildPagecutDescription(pagecut),
            _ => "Unknown Command"
        };
    }

    private static string BuildPagecutDescription(DomainElements.Pagecut pagecut)
    {
        var modeLabel = pagecut.Mode switch
        {
            DomainElements.PagecutMode.Full => "full",
            DomainElements.PagecutMode.Partial => "partial",
            DomainElements.PagecutMode.PartialOnePoint => "partial one point",
            DomainElements.PagecutMode.PartialThreePoint => "partial three point",
            _ => "unknown"
        };

        return pagecut.FeedMotionUnits.HasValue
            ? $"Pagecut ({modeLabel}, feed={pagecut.FeedMotionUnits.Value})"
            : $"Pagecut ({modeLabel})";
    }
}
