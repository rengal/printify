using Printify.Domain.Media;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing.EscPos;

/// <summary>
/// Command-agnostic rendered image metadata produced during media processing.
/// </summary>
/// <param name="Width">Rendered image width in printer dots.</param>
/// <param name="Height">Rendered image height in printer dots.</param>
/// <param name="Media">Rendered image media payload.</param>
public sealed record RenderedImageMedia(
    int Width,
    int Height,
    MediaUpload Media);

/// <summary>
/// ESC/POS-specific barcode and QR code generation service.
/// Generates images from barcode/QR commands during parsing.
/// </summary>
public interface IEscPosBarcodeService
{
    /// <summary>
    /// Generates a barcode image using the supplied payload and rendering options.
    /// </summary>
    RenderedImageMedia GenerateEscPosBarcodeMedia(EscPosPrintBarcodeUpload upload, BarcodeRenderOptions options);

    /// <summary>
    /// Generates a QR code image using the supplied payload and rendering options.
    /// </summary>
    RenderedImageMedia GenerateQrMedia(QrRenderOptions options);
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
