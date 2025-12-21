using Printify.Domain.Mapping;

namespace Printify.Web.Mapping;

using System;
using System.Collections.Generic;
using System.Linq;
using Printify.Domain.Documents;
using Printify.Domain.Media;
using Printify.Web.Contracts.Documents.Responses;
using DomainElements = Printify.Domain.Documents.Elements;
using ResponseElements = Printify.Web.Contracts.Documents.Responses.Elements;

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
            DomainElements.Bell => new ResponseElements.Bell(),
            DomainElements.Error error => new ResponseElements.ErrorDto(error.Code, error.Message),
            DomainElements.Pagecut => new ResponseElements.PagecutDto(),
            DomainElements.PrinterError printerError => new ResponseElements.ErrorDto(string.Empty, printerError.Message),
            DomainElements.GetPrinterStatus status => new ResponseElements.PrinterStatusDto(
                status.StatusByte,
                status.AdditionalStatusByte.HasValue ? $"0x{status.AdditionalStatusByte.Value:X2}" : null),
            DomainElements.PrintBarcode barcode => new ResponseElements.PrintBarcodeDto(
                DomainMapper.ToString(barcode.Symbology),
                barcode.Width,
                barcode.Height,
                ToMediaDto(barcode.Media)),
            DomainElements.PrintQrCode qr => new ResponseElements.PrintQrCodeDto(
                qr.Data,
                qr.Width,
                qr.Height,
                ToMediaDto(qr.Media)),
            DomainElements.Pulse pulse => new ResponseElements.PulseDto(
                pulse.Pin,
                pulse.OnTimeMs,
                pulse.OffTimeMs),
            DomainElements.RasterImage raster => new ResponseElements.RasterImageDto(
                raster.Width,
                raster.Height,
                ToMediaDto(raster.Media)),
            DomainElements.ResetPrinter => new ResponseElements.ResetPrinterDto(),
            DomainElements.SetBarcodeHeight height => new ResponseElements.SetBarcodeHeightDto(height.HeightInDots),
            DomainElements.SetBarcodeLabelPosition position => new ResponseElements.SetBarcodeLabelPositionDto(
                DomainMapper.ToString(position.Position)),
            DomainElements.SetBarcodeModuleWidth moduleWidth => new ResponseElements.SetBarcodeModuleWidthDto(moduleWidth.ModuleWidth),
            DomainElements.SetBoldMode bold => new ResponseElements.SetBoldModeDto(bold.IsEnabled),
            DomainElements.SetCodePage codePage => new ResponseElements.SetCodePageDto(codePage.CodePage),
            DomainElements.SetFont font => new ResponseElements.SetFontDto(font.FontNumber, font.IsDoubleWidth, font.IsDoubleHeight),
            DomainElements.SetJustification justification => new ResponseElements.SetJustificationDto(DomainMapper.ToString(justification.Justification)),
            DomainElements.SetLineSpacing spacing => new ResponseElements.SetLineSpacingDto(spacing.Spacing),
            DomainElements.ResetLineSpacing => new ResponseElements.ResetLineSpacingDto(),
            DomainElements.SetQrErrorCorrection correction => new ResponseElements.SetQrErrorCorrectionDto(DomainMapper.ToString(correction.Level)),
            DomainElements.SetQrModel model => new ResponseElements.SetQrModelDto(DomainMapper.ToString(model.Model)),
            DomainElements.SetQrModuleSize moduleSize => new ResponseElements.SetQrModuleSizeDto(moduleSize.ModuleSize),
            DomainElements.SetReverseMode reverse => new ResponseElements.SetReverseModeDto(reverse.IsEnabled),
            DomainElements.SetUnderlineMode underline => new ResponseElements.SetUnderlineModeDto(underline.IsEnabled),
            DomainElements.StoreQrData store => new ResponseElements.StoreQrDataDto(store.Content),
            DomainElements.StoredLogo storedLogo => new ResponseElements.StoredLogoDto(storedLogo.LogoId),
            DomainElements.TextLine textLine => new ResponseElements.TextLineDto(textLine.Text),
            _ => throw new NotSupportedException($"Element type '{element.GetType().FullName}' is not supported in responses.")
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
}
