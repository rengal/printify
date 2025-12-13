using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using Printify.Domain.Printers;

namespace Printify.Application.Interfaces;

/// <summary>
/// Converts raw bitmap data to MediaUpload format.
/// Implementation can use any image library (SkiaSharp, System.Drawing, ImageSharp, etc.).
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Converts a monochrome bitmap to MediaUpload with specified format.
    /// </summary>
    /// <param name="bitmap">Raw bitmap data.</param>
    /// <param name="format">Target image format (e.g., "image/png", "image/jpeg").</param>
    /// <returns>MediaUpload ready for transmission or storage.</returns>
    MediaUpload ConvertToMediaUpload(MonochromeBitmap bitmap, string format = "image/png");

    /// <summary>
    /// Generates a barcode image using the supplied payload and rendering options.
    /// </summary>
    RasterImageUpload GenerateBarcodeMedia(PrintBarcodeUpload upload, BarcodeRenderOptions options);

    /// <summary>
    /// Generates a QR code image using the supplied payload and rendering options.
    /// </summary>
    RasterImageUpload GenerateQrMedia(QrRenderOptions options);
}

public sealed record BarcodeRenderOptions(
    int? HeightInDots,
    int? ModuleWidthInDots,
    BarcodeLabelPosition? LabelPosition,
    TextJustification? Justification,
    int? PrinterWidthInDots);

public sealed record QrRenderOptions(
    string Data,
    QrModel Model,
    int? ModuleSizeInDots,
    QrErrorCorrectionLevel? ErrorCorrectionLevel,
    TextJustification? Justification,
    int? PrinterWidthInDots);
