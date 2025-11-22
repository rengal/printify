namespace Printify.Web.Mapping;

using System;
using System.Collections.Generic;
using System.Linq;
using Printify.Domain.Documents;
using Printify.Domain.Media;
using Printify.Domain.Printers;
using Printify.Web.Contracts.Documents.Responses;
using Printify.Web.Contracts.Documents.Shared.Elements;
using WebElements = Printify.Web.Contracts.Documents.Requests.Elements;
using DomainElements = Printify.Domain.Documents.Elements;
using ResponseElements = Printify.Web.Contracts.Documents.Responses.Elements;

internal static class DocumentMapper
{
    internal static DocumentDto ToResponseDto(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var responseElements = document.Elements?
            .Select(ToResponseElement)
            .ToList()
            ?? new List<ResponseElements.ResponseElement>();

        return new DocumentDto(
            document.Id,
            document.PrintJobId,
            document.PrinterId,
            document.CreatedAt,
            MapProtocol(document.Protocol),
            document.ClientAddress,
            responseElements.AsReadOnly());
    }

    private static ResponseElements.ResponseElement ToResponseElement(DomainElements.Element element)
    {
        return element switch
        {
            DomainElements.Bell => new ResponseElements.Bell(),
            DomainElements.Error error => new ResponseElements.Error(error.Code, error.Message),
            DomainElements.Pagecut => new ResponseElements.Pagecut(),
            DomainElements.PrinterError printerError => new ResponseElements.PrinterError(printerError.Message),
            DomainElements.GetPrinterStatus status => new ResponseElements.PrinterStatus(
                status.StatusByte,
                status.AdditionalStatusByte.HasValue ? $"0x{status.AdditionalStatusByte.Value:X2}" : null),
            DomainElements.PrintBarcode => throw new NotSupportedException("PrintBarcode elements must be rendered before publishing."),
            DomainElements.PrintQrCode => throw new NotSupportedException("PrintQrCode elements must be rendered before publishing."),
            DomainElements.Pulse pulse => new ResponseElements.Pulse(
                MapPulsePin(pulse.Pin),
                pulse.OnTimeMs,
                pulse.OffTimeMs),
            DomainElements.RasterImage raster => new ResponseElements.RasterImageDto(
                raster.Width,
                raster.Height,
                ToMediaDto(raster.Media)),
            DomainElements.ResetPrinter => new ResponseElements.ResetPrinter(),
            DomainElements.SetBarcodeHeight height => new ResponseElements.SetBarcodeHeight(height.HeightInDots),
            DomainElements.SetBarcodeLabelPosition position => new ResponseElements.SetBarcodeLabelPosition(
                MapBarcodeLabelPosition(position.Position)),
            DomainElements.SetBarcodeModuleWidth moduleWidth => new ResponseElements.SetBarcodeModuleWidth(moduleWidth.ModuleWidth),
            DomainElements.SetBoldMode bold => new ResponseElements.SetBoldMode(bold.IsEnabled),
            DomainElements.SetCodePage codePage => new ResponseElements.SetCodePage(codePage.CodePage),
            DomainElements.SetFont font => new ResponseElements.SetFont(font.FontNumber, font.IsDoubleWidth, font.IsDoubleHeight),
            DomainElements.SetJustification justification => new ResponseElements.SetJustification(MapTextJustification(justification.Justification)),
            DomainElements.SetLineSpacing spacing => new ResponseElements.SetLineSpacing(spacing.Spacing),
            DomainElements.ResetLineSpacing => new ResponseElements.ResetLineSpacing(),
            DomainElements.SetQrErrorCorrection correction => new ResponseElements.SetQrErrorCorrection(MapQrErrorCorrectionLevel(correction.Level)),
            DomainElements.SetQrModel model => new ResponseElements.SetQrModel(MapQrModel(model.Model)),
            DomainElements.SetQrModuleSize moduleSize => new ResponseElements.SetQrModuleSize(moduleSize.ModuleSize),
            DomainElements.SetReverseMode reverse => new ResponseElements.SetReverseMode(reverse.IsEnabled),
            DomainElements.SetUnderlineMode underline => new ResponseElements.SetUnderlineMode(underline.IsEnabled),
            DomainElements.StoreQrData store => new ResponseElements.StoreQrData(store.Content),
            DomainElements.StoredLogo storedLogo => new ResponseElements.StoredLogo(storedLogo.LogoId),
            DomainElements.TextLine textLine => new ResponseElements.TextLine(textLine.Text),
            _ => throw new NotSupportedException($"Element type '{element.GetType().FullName}' is not supported in responses.")
        };
    }

    private static DomainElements.Element ToDomainElement(BaseElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element switch
        {
            WebElements.Bell => new DomainElements.Bell(),
            WebElements.Error error => new DomainElements.Error(error.Code, error.Message),
            WebElements.Pagecut => new DomainElements.Pagecut(DomainElements.PagecutMode.Full),
            WebElements.PrinterError printerError => new DomainElements.PrinterError(printerError.Message),
            WebElements.PrinterStatus printerStatus => new DomainElements.GetPrinterStatus(printerStatus.StatusByte),
            WebElements.PrintBarcode barcode => new DomainElements.PrintBarcode(
                ParseBarcodeSymbology(barcode.Symbology),
                barcode.Data),
            WebElements.PrintQrCode => new DomainElements.PrintQrCode(),
            WebElements.Pulse pulse => new DomainElements.Pulse(
                ParsePulsePin(pulse.Pin),
                pulse.OnTimeMs,
                pulse.OffTimeMs),
            WebElements.ResetPrinter => new DomainElements.ResetPrinter(),
            WebElements.SetBarcodeHeight barcodeHeight => new DomainElements.SetBarcodeHeight(barcodeHeight.HeightInDots),
            WebElements.SetBarcodeLabelPosition labelPosition => new DomainElements.SetBarcodeLabelPosition(
                ParseBarcodeLabelPosition(labelPosition.Position)),
            WebElements.SetBarcodeModuleWidth moduleWidth => new DomainElements.SetBarcodeModuleWidth(moduleWidth.ModuleWidth),
            WebElements.SetBoldMode boldMode => new DomainElements.SetBoldMode(boldMode.IsEnabled),
            WebElements.SetCodePage codePage => new DomainElements.SetCodePage(codePage.CodePage),
            WebElements.SetFont font => new DomainElements.SetFont(font.FontNumber, font.IsDoubleWidth, font.IsDoubleHeight),
            WebElements.SetJustification justification => new DomainElements.SetJustification(
                ParseTextJustification(justification.Justification)),
            WebElements.SetLineSpacing spacing => new DomainElements.SetLineSpacing(spacing.Spacing),
            WebElements.ResetLineSpacing => new DomainElements.ResetLineSpacing(),
            WebElements.SetQrErrorCorrection errorCorrection => new DomainElements.SetQrErrorCorrection(
                ParseQrErrorCorrectionLevel(errorCorrection.Level)),
            WebElements.SetQrModel model => new DomainElements.SetQrModel(ParseQrModel(model.Model)),
            WebElements.SetQrModuleSize moduleSize => new DomainElements.SetQrModuleSize(moduleSize.ModuleSize),
            WebElements.SetReverseMode reverseMode => new DomainElements.SetReverseMode(reverseMode.IsEnabled),
            WebElements.SetUnderlineMode underlineMode => new DomainElements.SetUnderlineMode(underlineMode.IsEnabled),
            WebElements.StoreQrData storeQrData => new DomainElements.StoreQrData(storeQrData.Content),
            WebElements.StoredLogo storedLogo => new DomainElements.StoredLogo(storedLogo.LogoId),
            WebElements.TextLine textLine => new DomainElements.TextLine(textLine.Text),
            _ => throw new NotSupportedException($"Element type '{element.GetType().FullName}' is not supported.")
        };
    }

    private static Printify.Domain.Documents.Elements.BarcodeSymbology ParseBarcodeSymbology(BarcodeSymbology symbology)
        => Enum.Parse<Printify.Domain.Documents.Elements.BarcodeSymbology>(symbology.ToString(), true);

    private static Printify.Domain.Documents.Elements.BarcodeLabelPosition ParseBarcodeLabelPosition(BarcodeLabelPosition position)
        => Enum.Parse<Printify.Domain.Documents.Elements.BarcodeLabelPosition>(position.ToString(), true);

    private static Printify.Domain.Documents.Elements.PulsePin ParsePulsePin(PulsePin pin)
        => Enum.Parse<Printify.Domain.Documents.Elements.PulsePin>(pin.ToString(), true);

    private static Printify.Domain.Documents.Elements.QrErrorCorrectionLevel ParseQrErrorCorrectionLevel(QrErrorCorrectionLevel level)
        => Enum.Parse<Printify.Domain.Documents.Elements.QrErrorCorrectionLevel>(level.ToString(), true);

    private static Printify.Domain.Documents.Elements.QrModel ParseQrModel(QrModel model)
        => Enum.Parse<Printify.Domain.Documents.Elements.QrModel>(model.ToString(), true);

    private static Printify.Domain.Documents.Elements.TextJustification ParseTextJustification(TextJustification justification)
        => Enum.Parse<Printify.Domain.Documents.Elements.TextJustification>(justification.ToString(), true);

    private static BarcodeLabelPosition MapBarcodeLabelPosition(Printify.Domain.Documents.Elements.BarcodeLabelPosition position)
        => Enum.Parse<BarcodeLabelPosition>(position.ToString(), true);

    private static TextJustification MapTextJustification(Printify.Domain.Documents.Elements.TextJustification justification)
        => Enum.Parse<TextJustification>(justification.ToString(), true);

    private static PulsePin MapPulsePin(Printify.Domain.Documents.Elements.PulsePin pin)
        => Enum.Parse<PulsePin>(pin.ToString(), true);

    private static QrErrorCorrectionLevel MapQrErrorCorrectionLevel(Printify.Domain.Documents.Elements.QrErrorCorrectionLevel level)
        => Enum.Parse<QrErrorCorrectionLevel>(level.ToString(), true);

    private static QrModel MapQrModel(Printify.Domain.Documents.Elements.QrModel model)
        => Enum.Parse<QrModel>(model.ToString(), true);

    private static ResponseElements.MediaDto ToMediaDto(Media media)
    {
        ArgumentNullException.ThrowIfNull(media);

        return new ResponseElements.MediaDto(
            media.ContentType,
            media.Length,
            media.Checksum,
            CreateUri(media.Url));
    }

    private static Uri CreateUri(string href)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        if (Uri.TryCreate(href, UriKind.Relative, out var relative))
        {
            return relative;
        }

        return new Uri("/", UriKind.Relative);
    }

    private static string MapProtocol(Protocol protocol)
    {
        return protocol switch
        {
            Protocol.EscPos => "escpos",
            _ => protocol.ToString().ToLowerInvariant()
        };
    }
}
