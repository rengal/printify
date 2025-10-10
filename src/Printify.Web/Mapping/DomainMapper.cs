using System;
using System.Collections.Generic;
using System.Linq;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using Printify.Domain.Printers;
using Printify.Web.Contracts.Documents.Requests;
using Printify.Web.Contracts.Documents.Requests.Elements;
using Printify.Web.Contracts.Documents.Shared.Elements;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Users.Requests;

namespace Printify.Web.Mapping;

internal static class DomainMapper
{
    internal static SaveDocumentRequest ToSaveDocumentRequest(CreateDocumentRequest request, string? sourceIp)
    {
        ArgumentNullException.ThrowIfNull(request);

        var protocol = ParseProtocol(request.Protocol);
        var elements = request.Elements?.Select(ToDomainElement).ToList() ?? new List<Element>();

        return new SaveDocumentRequest(
            request.PrinterId,
            protocol,
            sourceIp,
            elements);
    }

    internal static SavePrinterRequest ToSavePrinterRequest(
        CreatePrinterRequest request,
        long? ownerUserId,
        long ownerSessionId,
        string createdFromIp)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdFromIp);

        return new SavePrinterRequest(
            ownerUserId,
            ownerSessionId,
            request.DisplayName,
            request.Protocol,
            request.WidthInDots,
            request.HeightInDots,
            createdFromIp);
    }

    internal static SavePrinterRequest ToSavePrinterRequest(
        UpdatePrinterRequest request,
        long? ownerUserId,
        long ownerSessionId,
        string createdFromIp)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdFromIp);

        return new SavePrinterRequest(
            ownerUserId,
            ownerSessionId,
            request.DisplayName,
            request.Protocol,
            request.WidthInDots,
            request.HeightInDots,
            createdFromIp);
    }

    internal static SaveUserRequest ToSaveUserRequest(CreateUserRequest request, string createdFromIp)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdFromIp);

        return new SaveUserRequest(request.DisplayName, createdFromIp);
    }

    private static Element ToDomainElement(RequestElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element switch
        {
            Requests.Elements.Bell bell => new Domain.Documents.Elements.Bell(bell.Sequence),
            Requests.Elements.Error error => new Domain.Documents.Elements.Error(error.Sequence, error.Code, error.Message),
            Requests.Elements.Pagecut pagecut => new Domain.Documents.Elements.Pagecut(pagecut.Sequence),
            Requests.Elements.PrinterError printerError => new Domain.Documents.Elements.PrinterError(printerError.Sequence, printerError.Message),
            Requests.Elements.PrinterStatus printerStatus => new Domain.Documents.Elements.PrinterStatus(
                printerStatus.Sequence,
                printerStatus.StatusByte,
                printerStatus.Description),
            Requests.Elements.PrintBarcode barcode => new Domain.Documents.Elements.PrintBarcode(
                barcode.Sequence,
                ParseBarcodeSymbology(barcode.Symbology),
                barcode.Data),
            Requests.Elements.PrintQrCode qr => new Domain.Documents.Elements.PrintQrCode(qr.Sequence, qr.Content),
            Requests.Elements.Pulse pulse => new Domain.Documents.Elements.Pulse(
                pulse.Sequence,
                ParsePulsePin(pulse.Pin),
                pulse.OnTimeMs,
                pulse.OffTimeMs),
            Requests.Elements.RasterImageContent raster => new RasterImageContent(
                raster.Sequence,
                raster.Width,
                raster.Height,
                new MediaContent(
                    raster.ContentType,
                    raster.SizeBytes,
                    raster.Sha256,
                    raster.Content)),
            Requests.Elements.ResetPrinter reset => new Domain.Documents.Elements.ResetPrinter(reset.Sequence),
            Requests.Elements.SetBarcodeHeight barcodeHeight => new Domain.Documents.Elements.SetBarcodeHeight(
                barcodeHeight.Sequence,
                barcodeHeight.HeightInDots),
            Requests.Elements.SetBarcodeLabelPosition labelPosition => new Domain.Documents.Elements.SetBarcodeLabelPosition(
                labelPosition.Sequence,
                ParseBarcodeLabelPosition(labelPosition.Position)),
            Requests.Elements.SetBarcodeModuleWidth moduleWidth => new Domain.Documents.Elements.SetBarcodeModuleWidth(
                moduleWidth.Sequence,
                moduleWidth.ModuleWidth),
            Requests.Elements.SetBoldMode boldMode => new Domain.Documents.Elements.SetBoldMode(
                boldMode.Sequence,
                boldMode.IsEnabled),
            Requests.Elements.SetCodePage codePage => new Domain.Documents.Elements.SetCodePage(codePage.Sequence, codePage.CodePage),
            Requests.Elements.SetFont font => new Domain.Documents.Elements.SetFont(
                font.Sequence,
                font.FontNumber,
                font.IsDoubleWidth,
                font.IsDoubleHeight),
            Requests.Elements.SetJustification justification => new Domain.Documents.Elements.SetJustification(
                justification.Sequence,
                ParseTextJustification(justification.Justification)),
            Requests.Elements.SetLineSpacing spacing => new Domain.Documents.Elements.SetLineSpacing(spacing.Sequence, spacing.Spacing),
            Requests.Elements.SetQrErrorCorrection errorCorrection => new Domain.Documents.Elements.SetQrErrorCorrection(
                errorCorrection.Sequence,
                ParseQrErrorCorrectionLevel(errorCorrection.Level)),
            Requests.Elements.SetQrModel model => new Domain.Documents.Elements.SetQrModel(
                model.Sequence,
                ParseQrModel(model.Model)),
            Requests.Elements.SetQrModuleSize moduleSize => new Domain.Documents.Elements.SetQrModuleSize(
                moduleSize.Sequence,
                moduleSize.ModuleSize),
            Requests.Elements.SetReverseMode reverseMode => new Domain.Documents.Elements.SetReverseMode(
                reverseMode.Sequence,
                reverseMode.IsEnabled),
            Requests.Elements.SetUnderlineMode underlineMode => new Domain.Documents.Elements.SetUnderlineMode(
                underlineMode.Sequence,
                underlineMode.IsEnabled),
            Requests.Elements.StoreQrData storeQrData => new Domain.Documents.Elements.StoreQrData(storeQrData.Sequence, storeQrData.Content),
            Requests.Elements.StoredLogo storedLogo => new Domain.Documents.Elements.StoredLogo(storedLogo.Sequence, storedLogo.LogoId),
            Requests.Elements.TextLine textLine => new Domain.Documents.Elements.TextLine(textLine.Sequence, textLine.Text),
            _ => throw new NotSupportedException($"Element type '{element.GetType().FullName}' is not supported.")
        };
    }

    private static Protocol ParseProtocol(string protocol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);

        if (Enum.TryParse(protocol, true, out Protocol parsed))
        {
            return parsed;
        }

        throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol is not supported.");
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
