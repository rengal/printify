using DomainElements = Printify.Domain.Documents.Elements;
using EscPosElements = Printify.Domain.Documents.Elements.EscPos;
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
    EscPosElements.RasterImageUpload GenerateBarcodeMedia(EscPosElements.PrintBarcodeUpload upload, BarcodeRenderOptions options);

    /// <summary>
    /// Generates a QR code image using the supplied payload and rendering options.
    /// </summary>
    EscPosElements.RasterImageUpload GenerateQrMedia(QrRenderOptions options);
}

public sealed record BarcodeRenderOptions(
    int? HeightInDots,
    int? ModuleWidthInDots,
    DomainElements.BarcodeLabelPosition? LabelPosition,
    DomainElements.TextJustification? Justification,
    int? PrinterWidthInDots);

public sealed record QrRenderOptions(
    string Data,
    DomainElements.QrModel Model,
    int? ModuleSizeInDots,
    DomainElements.QrErrorCorrectionLevel? ErrorCorrectionLevel,
    DomainElements.TextJustification? Justification,
    int? PrinterWidthInDots);
