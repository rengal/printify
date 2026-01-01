using Printify.Application.Interfaces;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace Printify.Infrastructure.Media;

/// <summary>
/// Converts monochrome bitmaps to media upload format using ImageSharp.
/// </summary>
public sealed class MediaService : IMediaService
{
    /// <inheritdoc />
    public MediaUpload ConvertToMediaUpload(MonochromeBitmap bitmap, string format = "image/png")
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(format);

        // Convert packed bits to ImageSharp RGBA image with transparency
        using var image = new Image<Rgba32>(bitmap.Width, bitmap.Height);

        for (int y = 0; y < bitmap.Height; y++)
        {
            var rowOffset = y * bitmap.Stride;
            Span<Rgba32> row = image.DangerousGetPixelRowMemory(y).Span;

            for (int x = 0; x < bitmap.Width; x++)
            {
                var byteIndex = rowOffset + (x / 8);
                var bitIndex = 7 - (x % 8); // MSB = leftmost pixel
                var isSet = (bitmap.Data[byteIndex] & (1 << bitIndex)) != 0;

                // Set bit (1) = black dot (printed), unset bit (0) = transparent (not printed)
                row[x] = isSet
                    ? new Rgba32(0, 0, 0, 255)      // Black opaque
                    : new Rgba32(255, 255, 255, 0); // Transparent
            }
        }

        // Encode to PNG format (transparency requires PNG)
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        var content = ms.ToArray();

        return new MediaUpload(
            ContentType: "image/png",
            Content: content
        );
    }

    public RasterImageUpload GenerateBarcodeMedia(PrintBarcodeUpload upload, BarcodeRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(upload);
        ArgumentNullException.ThrowIfNull(options);

        var targetHeight = options.HeightInDots.GetValueOrDefault(100);
        var moduleWidth = Math.Max(1, options.ModuleWidthInDots.GetValueOrDefault(2));
        var printerWidth = options.PrinterWidthInDots.GetValueOrDefault(Math.Max(200, moduleWidth * upload.Data.Length * 8));

        var rawWidth = Math.Clamp(moduleWidth * upload.Data.Length * 8, 64, printerWidth);
        var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
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
        var aligned = AlignToPrinter(image, printerWidth, options.Justification);
        var uploadMedia = EncodeImage(aligned);

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

        var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
        {
            Format = BarcodeFormat.QR_CODE,
            Options = qrOptions
        };

        using var image = writer.Write(data);
        ConvertWhiteToTransparent(image);
        var aligned = AlignToPrinter(image, printerWidth, options.Justification);
        var uploadMedia = EncodeImage(aligned);

        return new RasterImageUpload(aligned.Width, aligned.Height, uploadMedia);
    }

    private static MediaUpload EncodeImage(Image<Rgba32> image)
    {
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        var bytes = ms.ToArray();
        return new MediaUpload("image/png", bytes);
    }

    private static Image<Rgba32> AlignToPrinter(Image<Rgba32> source, int printerWidth, TextJustification? justification)
    {
        if (printerWidth <= source.Width || justification is null)
        {
            return source.Clone();
        }

        var offset = justification switch
        {
            TextJustification.Center => (printerWidth - source.Width) / 2,
            TextJustification.Right => printerWidth - source.Width,
            _ => 0
        };

        var canvas = new Image<Rgba32>(printerWidth, source.Height, Color.Transparent);

        canvas.Mutate(ctx =>
        {
            ctx.DrawImage(source, new Point(offset, 0), 1f);
        });

        return canvas;
    }

    /// <summary>
    /// Converts white or near-white pixels to transparent.
    /// Used to convert ZXing-generated images (black bars/modules on white background)
    /// to images with transparent background for thermal printing.
    /// </summary>
    private static void ConvertWhiteToTransparent(Image<Rgba32> image)
    {
        for (int y = 0; y < image.Height; y++)
        {
            var row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = row[x];
                // Check if pixel is white or near-white (threshold: 200 for R, G, B)
                if (pixel.R > 200 && pixel.G > 200 && pixel.B > 200)
                {
                    row[x] = new Rgba32(255, 255, 255, 0); // Transparent
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
