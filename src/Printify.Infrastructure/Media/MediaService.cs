using CodeGlyphX;
using CodeGlyphX.Rendering.Png;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

using Printify.Application.Interfaces;
using Printify.Domain.Media;
using Printify.Infrastructure.Printing.EscPos;
using Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.Infrastructure.Printing.Epl;

namespace Printify.Infrastructure.Media;

/// <summary>
/// Converts monochrome bitmaps to media upload format using SkiaSharp.
/// Implements both generic media conversion and ESC/POS/EPL-specific barcode/QR generation.
/// </summary>
public sealed class MediaService : IMediaService, IEscPosBarcodeService, IEplBarcodeService
{
    private const int DefaultQrModuleSizeInDots = 4;

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

    public RenderedImageMedia GenerateEscPosBarcodeMedia(
        EscPosPrintBarcodeUpload upload,
        BarcodeRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(upload);
        ArgumentNullException.ThrowIfNull(options);

        var moduleWidth = Math.Max(1, options.ModuleWidthInDots.GetValueOrDefault(2));

        var targetHeight = options.HeightInDots.GetValueOrDefault(100);
        var printerWidth = options.PrinterWidthInDots.GetValueOrDefault(
            Math.Max(200, moduleWidth * upload.Data.Length * 8));
        var targetWidth = options.ModuleWidthInDots.HasValue
            ? Math.Clamp(moduleWidth * upload.Data.Length * 8, 64, printerWidth)
            : (int)(0.70 * options.PrinterWidthInDots.GetValueOrDefault());
        var writer = new BarcodeWriter
        {
            Format = MapEscPosSymbology(upload.Symbology),
            Options = new EncodingOptions
            {
                Height = targetHeight,
                Width = targetWidth,
                Margin = 0,
                PureBarcode = options.LabelPosition == EscPosBarcodeLabelPosition.NotPrinted
            }
        };

        using var image = writer.Write(upload.Data);
        ConvertWhiteToTransparent(image);
        using var aligned = EscPosRasterImageFitter.FitToPrinterWidth(image, printerWidth, options.Justification);
        var uploadMedia = EncodeMediaUpload(aligned);

        return new RenderedImageMedia(aligned.Width, aligned.Height, uploadMedia);
    }

    public RenderedImageMedia GenerateQrMedia(QrRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var data = options.Data ?? string.Empty;
        var moduleSize = Math.Max(2, options.ModuleSizeInDots.GetValueOrDefault(DefaultQrModuleSizeInDots));
        var qrOptions = new QrEasyOptions
        {
            ModuleSize = moduleSize,
            QuietZone = 0,
            ErrorCorrectionLevel = MapQrErrorCorrection(options.ErrorCorrectionLevel),
            TextEncoding = QrTextEncoding.Utf8,
            IncludeEci = true,
            Foreground = Rgba32.Black,
            Background = Rgba32.Transparent
        };

        var pixels = QrEasy.RenderPixels(data, out var width, out var height, out var stride, qrOptions);
        using var image = CreateSkiaBitmapFromRgbaPixels(pixels, width, height, stride);
        var printerWidth = options.PrinterWidthInDots.GetValueOrDefault(width);
        using var clipped = EscPosRasterImageFitter.ClipToPrinterWidth(image, printerWidth, options.Justification);
        var uploadMedia = EncodeMediaUpload(clipped);

        return new RenderedImageMedia(clipped.Width, clipped.Height, uploadMedia);
    }

    public MediaUpload GenerateEplBarcodeMedia(string type, string data, int width, int height, char hri)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(data);

        // Map EPL barcode type to ZXing BarcodeFormat
        var format = MapEplBarcodeType(type);
        var moduleWidth = Math.Max(1, width);

        // Estimate width based on data length and module width
        var rawWidth = Math.Clamp(moduleWidth * data.Length * 8, 64, 400);

        var writer = new BarcodeWriter
        {
            Format = format,
            Options = new EncodingOptions
            {
                Height = Math.Max(10, height),
                Width = rawWidth,
                Margin = 0,
                PureBarcode = hri == 'N' // No text if HRI is 'N' (none)
            }
        };

        using var image = writer.Write(data);
        ConvertWhiteToTransparent(image);
        return EncodeMediaUpload(image);
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

    private static SKBitmap CreateSkiaBitmapFromRgbaPixels(byte[] pixels, int width, int height, int stride)
    {
        var image = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        for (int y = 0; y < height; y++)
        {
            var rowOffset = y * stride;
            for (int x = 0; x < width; x++)
            {
                var pixelOffset = rowOffset + x * 4;

                // CodeGlyphX returns RGBA bytes; SkiaSharp receives explicit channel values for stable PNG output.
                image.SetPixel(x, y, new SKColor(
                    pixels[pixelOffset],
                    pixels[pixelOffset + 1],
                    pixels[pixelOffset + 2],
                    pixels[pixelOffset + 3]));
            }
        }

        return image;
    }

    private static BarcodeFormat MapEscPosSymbology(EscPosBarcodeSymbology symbology)
    {
        return symbology switch
        {
            EscPosBarcodeSymbology.UpcA => BarcodeFormat.UPC_A,
            EscPosBarcodeSymbology.UpcE => BarcodeFormat.UPC_E,
            EscPosBarcodeSymbology.Ean13 => BarcodeFormat.EAN_13,
            EscPosBarcodeSymbology.Ean8 => BarcodeFormat.EAN_8,
            EscPosBarcodeSymbology.Code39 => BarcodeFormat.CODE_39,
            EscPosBarcodeSymbology.Itf => BarcodeFormat.ITF,
            EscPosBarcodeSymbology.Codabar => BarcodeFormat.CODABAR,
            EscPosBarcodeSymbology.Code93 => BarcodeFormat.CODE_93,
            EscPosBarcodeSymbology.Code128 => BarcodeFormat.CODE_128,
            _ => BarcodeFormat.CODE_128
        };
    }

    /// <summary>
    /// Maps EPL barcode type strings to ZXing BarcodeFormat.
    /// EPL barcode types: 1=Code 39, 2=Code 39 with checksum, 3=EAN-8, 4=EAN-13,
    /// 5=UPC-A, 6=UPC-E, 7=Codabar, 8=Code 128, 9=Interleaved 2 of 5, etc.
    /// </summary>
    private static BarcodeFormat MapEplBarcodeType(string type)
    {
        // Handle both numeric and character-based type codes
        return type.ToUpperInvariant() switch
        {
            "1" or "A" => BarcodeFormat.CODE_39,  // Code 39
            "2" => BarcodeFormat.CODE_39,         // Code 39 with checksum
            "3" or "E8" => BarcodeFormat.EAN_8,   // EAN-8
            "4" or "E30" => BarcodeFormat.EAN_13, // EAN-13
            "5" or "UA" => BarcodeFormat.UPC_A,   // UPC-A
            "6" or "UE" => BarcodeFormat.UPC_E,   // UPC-E
            "7" or "C" => BarcodeFormat.CODABAR,  // Codabar
            "8" or "B" => BarcodeFormat.CODE_128, // Code 128
            "9" or "I" => BarcodeFormat.ITF,      // Interleaved 2 of 5
            _ => BarcodeFormat.CODE_128
        };
    }

    private static QrErrorCorrectionLevel MapQrErrorCorrection(EscPosQrErrorCorrectionLevel? level)
    {
        return level switch
        {
            EscPosQrErrorCorrectionLevel.Low => QrErrorCorrectionLevel.L,
            EscPosQrErrorCorrectionLevel.Medium => QrErrorCorrectionLevel.M,
            EscPosQrErrorCorrectionLevel.Quartile => QrErrorCorrectionLevel.Q,
            EscPosQrErrorCorrectionLevel.High => QrErrorCorrectionLevel.H,
            _ => QrErrorCorrectionLevel.M
        };
    }
}
