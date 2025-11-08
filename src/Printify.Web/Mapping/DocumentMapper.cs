using Printify.Domain.Media;
using Printify.Web.Contracts.Documents.Shared.Elements;
using WebElements = Printify.Web.Contracts.Documents.Requests.Elements;
using DomainElements = Printify.Domain.Documents.Elements;

namespace Printify.Web.Mapping;

internal static class DocumentMapper
{
    private static Domain.Documents.Elements.Element ToDomainElement(BaseElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element switch
        {
            WebElements.Bell => new DomainElements.Bell(),
            WebElements.Error error => new DomainElements.Error( error.Code, error.Message),
            WebElements.Pagecut => new DomainElements.Pagecut(),
            WebElements.PrinterError printerError => new DomainElements.PrinterError(printerError.Message),
            WebElements.PrinterStatus printerStatus => new DomainElements.GetPrinterStatus(
                printerStatus.StatusByte,
                printerStatus.Description),
            WebElements.PrintBarcode barcode => new DomainElements.PrintBarcode(
                ParseBarcodeSymbology(barcode.Symbology),
                barcode.Data),
            WebElements.PrintQrCode qr => new DomainElements.PrintQrCode(qr.Content),
            WebElements.Pulse pulse => new DomainElements.Pulse(
                ParsePulsePin(pulse.Pin),
                pulse.OnTimeMs,
                pulse.OffTimeMs),
            WebElements.RasterImageContent raster => new DomainElements.RasterImageContent(
                raster.Width,
                raster.Height,
                new MediaContent(
                    raster.ContentType,
                    raster.SizeBytes,
                    raster.Sha256,
                    raster.Content)),
            WebElements.ResetPrinter => new Domain.Documents.Elements.ResetPrinter(),
            WebElements.SetBarcodeHeight barcodeHeight => new Domain.Documents.Elements.SetBarcodeHeight(
                barcodeHeight.HeightInDots),
            WebElements.SetBarcodeLabelPosition labelPosition => new Domain.Documents.Elements.SetBarcodeLabelPosition(
                ParseBarcodeLabelPosition(labelPosition.Position)),
            WebElements.SetBarcodeModuleWidth moduleWidth => new Domain.Documents.Elements.SetBarcodeModuleWidth(
                moduleWidth.ModuleWidth),
            WebElements.SetBoldMode boldMode => new Domain.Documents.Elements.SetBoldMode(
                boldMode.IsEnabled),
            WebElements.SetCodePage codePage => new Domain.Documents.Elements.SetCodePage(codePage.CodePage),
            WebElements.SetFont font => new Domain.Documents.Elements.SetFont(
                font.FontNumber,
                font.IsDoubleWidth,
                font.IsDoubleHeight),
            WebElements.SetJustification justification => new Domain.Documents.Elements.SetJustification(
                ParseTextJustification(justification.Justification)),
            WebElements.SetLineSpacing spacing => new Domain.Documents.Elements.SetLineSpacing(spacing.Spacing),
            WebElements.ResetLineSpacing => new Domain.Documents.Elements.ResetLineSpacing(),
            WebElements.SetQrErrorCorrection errorCorrection => new Domain.Documents.Elements.SetQrErrorCorrection(
                ParseQrErrorCorrectionLevel(errorCorrection.Level)),
            WebElements.SetQrModel model => new Domain.Documents.Elements.SetQrModel(
                ParseQrModel(model.Model)),
            WebElements.SetQrModuleSize moduleSize => new Domain.Documents.Elements.SetQrModuleSize(
                moduleSize.ModuleSize),
            WebElements.SetReverseMode reverseMode => new Domain.Documents.Elements.SetReverseMode(
                reverseMode.IsEnabled),
            WebElements.SetUnderlineMode underlineMode => new Domain.Documents.Elements.SetUnderlineMode(
                underlineMode.IsEnabled),
            WebElements.StoreQrData storeQrData => new Domain.Documents.Elements.StoreQrData(storeQrData.Content),
            WebElements.StoredLogo storedLogo => new Domain.Documents.Elements.StoredLogo(storedLogo.LogoId),
            WebElements.TextLine textLine => new Domain.Documents.Elements.TextLine(textLine.Text),
            _ => throw new NotSupportedException($"Element type '{element.GetType().FullName}' is not supported.")
        };
    }

    private static Printify.Domain.Documents.Elements.BarcodeSymbology ParseBarcodeSymbology(BarcodeSymbology symbology)
    {
        return Enum.Parse<Printify.Domain.Documents.Elements.BarcodeSymbology>(symbology.ToString(), true);
    }

    private static Printify.Domain.Documents.Elements.BarcodeLabelPosition ParseBarcodeLabelPosition(BarcodeLabelPosition position)
    {
        return Enum.Parse<Printify.Domain.Documents.Elements.BarcodeLabelPosition>(position.ToString(), true);
    }

    private static Printify.Domain.Documents.Elements.PulsePin ParsePulsePin(PulsePin pin)
    {
        return Enum.Parse<Printify.Domain.Documents.Elements.PulsePin>(pin.ToString(), true);
    }

    private static Printify.Domain.Documents.Elements.QrErrorCorrectionLevel ParseQrErrorCorrectionLevel(QrErrorCorrectionLevel level)
    {
        return Enum.Parse<Printify.Domain.Documents.Elements.QrErrorCorrectionLevel>(level.ToString(), true);
    }

    private static Printify.Domain.Documents.Elements.QrModel ParseQrModel(QrModel model)
    {
        return Enum.Parse<Printify.Domain.Documents.Elements.QrModel>(model.ToString(), true);
    }

    private static Printify.Domain.Documents.Elements.TextJustification ParseTextJustification(TextJustification justification)
    {
        return Enum.Parse<Printify.Domain.Documents.Elements.TextJustification>(justification.ToString(), true);
    }
}
