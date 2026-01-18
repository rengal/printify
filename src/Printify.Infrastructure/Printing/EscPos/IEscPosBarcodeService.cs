using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing.EscPos;

/// <summary>
/// ESC/POS-specific barcode and QR code generation service.
/// Generates images from barcode/QR commands during parsing.
/// </summary>
public interface IEscPosBarcodeService
{
    /// <summary>
    /// Generates a barcode image using the supplied payload and rendering options.
    /// </summary>
    RasterImageUpload GenerateBarcodeMedia(PrintBarcodeUpload upload, BarcodeRenderOptions options);

    /// <summary>
    /// Generates a QR code image using the supplied payload and rendering options.
    /// </summary>
    RasterImageUpload GenerateQrMedia(QrRenderOptions options);
}

/// <summary>
/// Rendering options for barcode generation.
/// </summary>
public sealed record BarcodeRenderOptions(
    int? HeightInDots,
    int? ModuleWidthInDots,
    BarcodeLabelPosition? LabelPosition,
    TextJustification? Justification,
    int? PrinterWidthInDots);

/// <summary>
/// Rendering options for QR code generation.
/// </summary>
public sealed record QrRenderOptions(
    string Data,
    QrModel Model,
    int? ModuleSizeInDots,
    QrErrorCorrectionLevel? ErrorCorrectionLevel,
    TextJustification? Justification,
    int? PrinterWidthInDots);
