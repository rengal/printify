using System;
using System.Collections.Generic;
using System.Linq;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using Printify.Domain.Printers;
using Printify.Domain.Users;
using Printify.Web.Contracts.Documents.Responses;
using Printify.Web.Contracts.Documents.Responses.Elements;
using Printify.Web.Contracts.Documents.Shared.Elements;
using Printify.Web.Contracts.Printers.Responses;
using Printify.Web.Contracts.Users.Responses;

namespace Printify.Web.Mapping;

internal static class ContractMapper
{
    internal static UserDto ToDto(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserDto(user.Id, user.DisplayName);
    }

    internal static PrinterDto ToPrinterDto(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        return new PrinterDto(
            printer.Id,
            printer.DisplayName,
            printer.Protocol,
            printer.WidthInDots,
            printer.HeightInDots);
    }

    internal static IReadOnlyList<PrinterDto> ToPrinterDtos(IEnumerable<Printer> printers)
    {
        ArgumentNullException.ThrowIfNull(printers);
        return printers.Select(ToPrinterDto).ToList();
    }

    internal static DocumentDto ToDocumentDto(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var elements = document.Elements.Count == 0
            ? Array.Empty<ResponseElement>()
            : document.Elements.Select(element => ToResponseElement(document.Id, element)).ToArray();

        return new DocumentDto(
            document.Id,
            document.PrinterId,
            document.Timestamp,
            document.Protocol.ToString(),
            elements);
    }

    internal static DocumentDto ToDocumentDto(DocumentDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return new DocumentDto(
            descriptor.Id,
            descriptor.PrinterId,
            descriptor.Timestamp,
            descriptor.Protocol.ToString(),
            Array.Empty<ResponseElement>());
    }

    private static ResponseElement ToResponseElement(long documentId, Element element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element switch
        {
            Bell bell => new Responses.Elements.Bell(bell.Sequence),
            Error error => new Responses.Elements.Error(error.Sequence, error.Code, error.Message),
            Pagecut pagecut => new Responses.Elements.Pagecut(pagecut.Sequence),
            PrinterError printerError => new Responses.Elements.PrinterError(printerError.Sequence, printerError.Message),
            PrinterStatus printerStatus => new Responses.Elements.PrinterStatus(
                printerStatus.Sequence,
                printerStatus.StatusByte,
                printerStatus.Description),
            PrintBarcode barcode => new Responses.Elements.PrintBarcode(
                barcode.Sequence,
                MapBarcodeSymbology(barcode.Symbology),
                "image/png",
                null,
                null,
                BuildElementUri(documentId, barcode.Sequence, "barcode")),
            PrintQrCode qr => new Responses.Elements.PrintQrCode(
                qr.Sequence,
                "image/png",
                null,
                null,
                BuildElementUri(documentId, qr.Sequence, "qr")),
            Pulse pulse => new Responses.Elements.Pulse(
                pulse.Sequence,
                MapPulsePin(pulse.Pin),
                pulse.OnTimeMs,
                pulse.OffTimeMs),
            RasterImageDescriptor descriptor => MapRasterDescriptor(descriptor, documentId),
            RasterImageContent content => MapRasterContent(content),
            ResetPrinter reset => new Responses.Elements.ResetPrinter(reset.Sequence),
            SetBarcodeHeight barcodeHeight => new Responses.Elements.SetBarcodeHeight(barcodeHeight.Sequence, barcodeHeight.HeightInDots),
            SetBarcodeLabelPosition labelPosition => new Responses.Elements.SetBarcodeLabelPosition(
                labelPosition.Sequence,
                MapBarcodeLabelPosition(labelPosition.Position)),
            SetBarcodeModuleWidth moduleWidth => new Responses.Elements.SetBarcodeModuleWidth(
                moduleWidth.Sequence,
                moduleWidth.ModuleWidth),
            SetBoldMode boldMode => new Responses.Elements.SetBoldMode(boldMode.Sequence, boldMode.IsEnabled),
            SetCodePage codePage => new Responses.Elements.SetCodePage(codePage.Sequence, codePage.CodePage),
            SetFont font => new Responses.Elements.SetFont(
                font.Sequence,
                font.FontNumber,
                font.IsDoubleWidth,
                font.IsDoubleHeight),
            SetJustification justification => new Responses.Elements.SetJustification(
                justification.Sequence,
                MapTextJustification(justification.Justification)),
            SetLineSpacing spacing => new Responses.Elements.SetLineSpacing(spacing.Sequence, spacing.Spacing),
            SetQrErrorCorrection errorCorrection => new Responses.Elements.SetQrErrorCorrection(
                errorCorrection.Sequence,
                MapQrErrorCorrectionLevel(errorCorrection.Level)),
            SetQrModel model => new Responses.Elements.SetQrModel(
                model.Sequence,
                MapQrModel(model.Model)),
            SetQrModuleSize qrModuleSize => new Responses.Elements.SetQrModuleSize(
                qrModuleSize.Sequence,
                qrModuleSize.ModuleSize),
            SetReverseMode reverseMode => new Responses.Elements.SetReverseMode(
                reverseMode.Sequence,
                reverseMode.IsEnabled),
            SetUnderlineMode underlineMode => new Responses.Elements.SetUnderlineMode(
                underlineMode.Sequence,
                underlineMode.IsEnabled),
            StoreQrData storeQrData => new Responses.Elements.StoreQrData(storeQrData.Sequence, storeQrData.Content),
            StoredLogo storedLogo => new Responses.Elements.StoredLogo(storedLogo.Sequence, storedLogo.LogoId),
            TextLine line => new Responses.Elements.TextLine(line.Sequence, line.Text),
            _ => new Responses.Elements.Error(element.Sequence, "UnsupportedElement", element.GetType().FullName ?? "unknown")
        };
    }

    private static Responses.Elements.RasterImageDescriptor MapRasterDescriptor(RasterImageDescriptor descriptor, long documentId)
    {
        var meta = descriptor.Media.Meta;
        var href = TryCreateUri(descriptor.Media.Url) ?? BuildElementUri(documentId, descriptor.Sequence, "raster");

        return new Responses.Elements.RasterImageDescriptor(
            descriptor.Sequence,
            descriptor.Width,
            descriptor.Height,
            meta.ContentType,
            meta.Length,
            meta.Checksum,
            href);
    }

    private static Responses.Elements.RasterImageDescriptor MapRasterContent(RasterImageContent content)
    {
        var media = content.Media;
        var href = BuildDataUri(media.ContentType, media.Content);

        return new Responses.Elements.RasterImageDescriptor(
            content.Sequence,
            content.Width,
            content.Height,
            media.ContentType,
            media.Length,
            media.Checksum,
            href);
    }

    private static Uri BuildElementUri(long documentId, int sequence, string resourceKind)
    {
        return new Uri($"/api/documents/{documentId}/elements/{sequence}/{resourceKind}", UriKind.Relative);
    }

    private static Uri BuildDataUri(string contentType, ReadOnlyMemory<byte>? content)
    {
        if (content is null || content.Value.IsEmpty)
        {
            return new Uri("about:blank");
        }

        var base64 = Convert.ToBase64String(content.Value.ToArray());
        return new Uri($"data:{contentType};base64,{base64}", UriKind.Absolute);
    }

    private static Uri? TryCreateUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
    }

    private static BarcodeSymbology MapBarcodeSymbology(Printify.Domain.Documents.Elements.BarcodeSymbology symbology)
    {
        return Enum.Parse<BarcodeSymbology>(symbology.ToString(), true);
    }

    private static BarcodeLabelPosition MapBarcodeLabelPosition(Printify.Domain.Documents.Elements.BarcodeLabelPosition position)
    {
        return Enum.Parse<BarcodeLabelPosition>(position.ToString(), true);
    }

    private static PulsePin MapPulsePin(Printify.Domain.Documents.Elements.PulsePin pin)
    {
        return Enum.Parse<PulsePin>(pin.ToString(), true);
    }

    private static QrErrorCorrectionLevel MapQrErrorCorrectionLevel(Printify.Domain.Documents.Elements.QrErrorCorrectionLevel level)
    {
        return Enum.Parse<QrErrorCorrectionLevel>(level.ToString(), true);
    }

    private static QrModel MapQrModel(Printify.Domain.Documents.Elements.QrModel model)
    {
        return Enum.Parse<QrModel>(model.ToString(), true);
    }

    private static TextJustification MapTextJustification(Printify.Domain.Documents.Elements.TextJustification justification)
    {
        return Enum.Parse<TextJustification>(justification.ToString(), true);
    }
}
