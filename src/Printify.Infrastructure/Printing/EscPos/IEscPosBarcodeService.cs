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
    EscPosRasterImageUpload GenerateBarcodeMedia(EscPosPrintBarcodeUpload upload, BarcodeRenderOptions options);

    /// <summary>
    /// Generates a QR code image using the supplied payload and rendering options.
    /// </summary>
    EscPosRasterImageUpload GenerateQrMedia(QrRenderOptions options);
}

/// <summary>
/// Rendering options for barcode generation.
/// </summary>
public sealed record BarcodeRenderOptions(
    int? HeightInDots,
    int? ModuleWidthInDots,
    EscPosBarcodeLabelPosition? LabelPosition,
    EscPosTextJustification? Justification,
    int? PrinterWidthInDots);

/// <summary>
/// Rendering options for QR code generation.
/// </summary>
public sealed record QrRenderOptions(
    string Data,
    EscPosQrModel Model,
    int? ModuleSizeInDots,
    EscPosQrErrorCorrectionLevel? ErrorCorrectionLevel,
    EscPosTextJustification? Justification,
    int? PrinterWidthInDots);
