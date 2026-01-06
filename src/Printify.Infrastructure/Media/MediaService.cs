using Printify.Application.Interfaces;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.SkiaSharp;

namespace Printify.Infrastructure.Media;

/// <summary>
/// Converts monochrome bitmaps to media upload format using SkiaSharp.
/// </summary>
public sealed class MediaService : IMediaService
{
    /// <inheritdoc />
    public MediaUpload ConvertToMediaUpload(MonochromeBitmap bitmap, string format = "image/png")
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(format);

        // Convert packed bits to a SkiaSharp RGBA bitmap with transparency.
        using var image = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        for (int y = 0; y < bitmap.Height; y++)
        {
            var rowOffset = y * bitmap.Stride;
            for (int x = 0; x < bitmap.Width; x++)
            {
                var byteIndex = rowOffset + (x / 8);
                var bitIndex = 7 - (x % 8); // MSB = leftmost pixel
                var isSet = (bitmap.Data[byteIndex] & (1 << bitIndex)) != 0;

                // Set bit (1) = black dot (printed), unset bit (0) = transparent (not printed)
                image.SetPixel(x, y, isSet ? new SKColor(0, 0, 0, 255) : new SKColor(255, 255, 255, 0));
            }
        }

        var content = EncodePng(image);
        return new MediaUpload("image/png", content);
    }

    public RasterImageUpload GenerateBarcodeMedia(PrintBarcodeUpload upload, BarcodeRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(upload);
        ArgumentNullException.ThrowIfNull(options);

        var targetHeight = options.HeightInDots.GetValueOrDefault(100);
        var moduleWidth = Math.Max(1, options.ModuleWidthInDots.GetValueOrDefault(2));
        var printerWidth = options.PrinterWidthInDots.GetValueOrDefault(Math.Max(200, moduleWidth * upload.Data.Length * 8));

        var rawWidth = Math.Clamp(moduleWidth * upload.Data.Length * 8, 64, printerWidth);
        var writer = new BarcodeWriter
        {
            Format = MapSymbology(upload.Symbology),
            Options = new EncodingOptions
            {
                Height = targetHeight,
                Width = rawWidth,
                Margin = 0,
                PureBarcode = options.LabelPosition == BarcodeLabelPosition.NotPrinted
            }
        };

        using var image = writer.Write(upload.Data);
        ConvertWhiteToTransparent(image);
        using var aligned = AlignToPrinter(image, printerWidth, options.Justification);
        var uploadMedia = EncodeMediaUpload(aligned);

        return new RasterImageUpload(aligned.Width, aligned.Height, uploadMedia);
    }

    public RasterImageUpload GenerateQrMedia(QrRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var data = options.Data ?? string.Empty;
        var moduleSize = Math.Max(2, options.ModuleSizeInDots.GetValueOrDefault(4));
        var targetSide = moduleSize * 25; // heuristic for typical QR version sizing
        var printerWidth = options.PrinterWidthInDots.GetValueOrDefault(targetSide);

        var qrOptions = new QrCodeEncodingOptions
        {
            Height = targetSide,
            Width = targetSide,
            Margin = 0,
            ErrorCorrection = MapErrorCorrection(options.ErrorCorrectionLevel),
            CharacterSet = "UTF-8"
        };

        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = qrOptions
        };

        using var image = writer.Write(data);
        ConvertWhiteToTransparent(image);
        using var aligned = AlignToPrinter(image, printerWidth, options.Justification);
        var uploadMedia = EncodeMediaUpload(aligned);

        return new RasterImageUpload(aligned.Width, aligned.Height, uploadMedia);
    }

    private static MediaUpload EncodeMediaUpload(SKBitmap bitmap)
    {
        var bytes = EncodePng(bitmap);
        return new MediaUpload("image/png", bytes);
    }

    private static byte[] EncodePng(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static SKBitmap AlignToPrinter(SKBitmap source, int printerWidth, TextJustification? justification)
    {
        if (printerWidth <= source.Width || justification is null)
        {
            return source.Copy();
        }

        var offset = justification switch
        {
            TextJustification.Center => (printerWidth - source.Width) / 2,
            TextJustification.Right => printerWidth - source.Width,
            _ => 0
        };

        var canvas = new SKBitmap(printerWidth, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var context = new SKCanvas(canvas);
        context.Clear(SKColors.Transparent);
        context.DrawBitmap(source, new SKPoint(offset, 0));
        return canvas;
    }

    /// <summary>
    /// Converts white or near-white pixels to transparent.
    /// Used to convert ZXing-generated images (black bars/modules on white background)
    /// to images with transparent background for thermal printing.
    /// </summary>
    private static void ConvertWhiteToTransparent(SKBitmap image)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image.GetPixel(x, y);
                // Check if pixel is white or near-white (threshold: 200 for R, G, B)
                if (pixel.Red > 200 && pixel.Green > 200 && pixel.Blue > 200)
                {
                    image.SetPixel(x, y, new SKColor(255, 255, 255, 0)); // Transparent
                }
            }
        }
    }

    private static BarcodeFormat MapSymbology(BarcodeSymbology symbology)
    {
        return symbology switch
        {
            BarcodeSymbology.UpcA => BarcodeFormat.UPC_A,
            BarcodeSymbology.UpcE => BarcodeFormat.UPC_E,
            BarcodeSymbology.Ean13 => BarcodeFormat.EAN_13,
            BarcodeSymbology.Ean8 => BarcodeFormat.EAN_8,
            BarcodeSymbology.Code39 => BarcodeFormat.CODE_39,
            BarcodeSymbology.Itf => BarcodeFormat.ITF,
            BarcodeSymbology.Codabar => BarcodeFormat.CODABAR,
            BarcodeSymbology.Code93 => BarcodeFormat.CODE_93,
            BarcodeSymbology.Code128 => BarcodeFormat.CODE_128,
            _ => BarcodeFormat.CODE_128
        };
    }

    private static ErrorCorrectionLevel MapErrorCorrection(QrErrorCorrectionLevel? level)
    {
        return level switch
        {
            QrErrorCorrectionLevel.Low => ErrorCorrectionLevel.L,
            QrErrorCorrectionLevel.Medium => ErrorCorrectionLevel.M,
            QrErrorCorrectionLevel.Quartile => ErrorCorrectionLevel.Q,
            QrErrorCorrectionLevel.High => ErrorCorrectionLevel.H,
            _ => ErrorCorrectionLevel.M
        };
    }
}
