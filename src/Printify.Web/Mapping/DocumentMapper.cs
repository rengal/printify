using Printify.Domain.Media;
using Printify.Domain.Printers;
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
            WebElements.Bell bell => new DomainElements.Bell(bell.Sequence),
            WebElements.Error error => new DomainElements.Error(error.Sequence, error.Code, error.Message),
            WebElements.Pagecut pagecut => new DomainElements.Pagecut(pagecut.Sequence),
            WebElements.PrinterError printerError => new DomainElements.PrinterError(printerError.Sequence, printerError.Message),
            WebElements.PrinterStatus printerStatus => new DomainElements.PrinterStatus(
                printerStatus.Sequence,
                printerStatus.StatusByte,
                printerStatus.Description),
            WebElements.PrintBarcode barcode => new DomainElements.PrintBarcode(
                barcode.Sequence,
                ParseBarcodeSymbology(barcode.Symbology),
                barcode.Data),
            WebElements.PrintQrCode qr => new DomainElements.PrintQrCode(qr.Sequence, qr.Content),
            WebElements.Pulse pulse => new DomainElements.Pulse(
                pulse.Sequence,
                ParsePulsePin(pulse.Pin),
                pulse.OnTimeMs,
                pulse.OffTimeMs),
            WebElements.RasterImageContent raster => new DomainElements.RasterImageContent(
                raster.Sequence,
                raster.Width,
                raster.Height,
                new MediaContent(
                    raster.ContentType,
                    raster.SizeBytes,
                    raster.Sha256,
                    raster.Content)),
            WebElements.ResetPrinter reset => new Domain.Documents.Elements.ResetPrinter(reset.Sequence),
            WebElements.SetBarcodeHeight barcodeHeight => new Domain.Documents.Elements.SetBarcodeHeight(
                barcodeHeight.Sequence,
                barcodeHeight.HeightInDots),
            WebElements.SetBarcodeLabelPosition labelPosition => new Domain.Documents.Elements.SetBarcodeLabelPosition(
                labelPosition.Sequence,
                ParseBarcodeLabelPosition(labelPosition.Position)),
            WebElements.SetBarcodeModuleWidth moduleWidth => new Domain.Documents.Elements.SetBarcodeModuleWidth(
                moduleWidth.Sequence,
                moduleWidth.ModuleWidth),
            WebElements.SetBoldMode boldMode => new Domain.Documents.Elements.SetBoldMode(
                boldMode.Sequence,
                boldMode.IsEnabled),
            WebElements.SetCodePage codePage => new Domain.Documents.Elements.SetCodePage(codePage.Sequence, codePage.CodePage),
            WebElements.SetFont font => new Domain.Documents.Elements.SetFont(
                font.Sequence,
                font.FontNumber,
                font.IsDoubleWidth,
                font.IsDoubleHeight),
            WebElements.SetJustification justification => new Domain.Documents.Elements.SetJustification(
                justification.Sequence,
                ParseTextJustification(justification.Justification)),
            WebElements.SetLineSpacing spacing => new Domain.Documents.Elements.SetLineSpacing(spacing.Sequence, spacing.Spacing),
            WebElements.SetQrErrorCorrection errorCorrection => new Domain.Documents.Elements.SetQrErrorCorrection(
                errorCorrection.Sequence,
                ParseQrErrorCorrectionLevel(errorCorrection.Level)),
            WebElements.SetQrModel model => new Domain.Documents.Elements.SetQrModel(
                model.Sequence,
                ParseQrModel(model.Model)),
            WebElements.SetQrModuleSize moduleSize => new Domain.Documents.Elements.SetQrModuleSize(
                moduleSize.Sequence,
                moduleSize.ModuleSize),
            WebElements.SetReverseMode reverseMode => new Domain.Documents.Elements.SetReverseMode(
                reverseMode.Sequence,
                reverseMode.IsEnabled),
            WebElements.SetUnderlineMode underlineMode => new Domain.Documents.Elements.SetUnderlineMode(
                underlineMode.Sequence,
                underlineMode.IsEnabled),
            WebElements.StoreQrData storeQrData => new Domain.Documents.Elements.StoreQrData(storeQrData.Sequence, storeQrData.Content),
            WebElements.StoredLogo storedLogo => new Domain.Documents.Elements.StoredLogo(storedLogo.Sequence, storedLogo.LogoId),
            WebElements.TextLine textLine => new Domain.Documents.Elements.TextLine(textLine.Sequence, textLine.Text),
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
