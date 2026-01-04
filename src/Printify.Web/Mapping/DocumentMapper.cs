using System;
using System.Collections.Generic;
using System.Linq;
using Printify.Application.Features.Printers.Documents;
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

        // Collect error messages from Error and PrinterError elements
        var errorMessages = document.Elements?
            .Where(e => e is DomainElements.Error or DomainElements.PrinterError)
            .Select(e => e switch
            {
                DomainElements.Error error => error.Message ?? error.Code ?? "Unknown error",
                DomainElements.PrinterError printerError => printerError.Message ?? "Printer error",
                _ => null
            })
            .Where(msg => msg is not null)
            .Cast<string>()
            .ToArray();

        return new DocumentDto(
            document.Id,
            document.PrintJobId,
            document.PrinterId,
            document.CreatedAt,
            DomainMapper.ToString(document.Protocol),
            document.WidthInDots,
            document.HeightInDots,
            document.ClientAddress,
            responseElements.AsReadOnly(),
            errorMessages is { Length: > 0 } ? errorMessages : null);
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
            DomainElements.LegacyCarriageReturn legacyCarriageReturn => WithCommandMetadata(
                new ResponseElements.LegacyCarriageReturnDto(),
                legacyCarriageReturn),
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
            CommandDescription = CommandDescriptionBuilder.Build(element),
            LengthInBytes = element.LengthInBytes
        };
    }
}
