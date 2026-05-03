using SkiaSharp;

using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Media;

public static class EscPosRasterImageFitter
{
    public static SKBitmap FitToPrinterWidth(
        SKBitmap source,
        int printerWidthInDots,
        EscPosTextJustification? justification)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (printerWidthInDots <= 0 || source.Width == printerWidthInDots)
        {
            return source.Copy();
        }

        var effectiveJustification = justification.GetValueOrDefault(EscPosTextJustification.Left);

        return source.Width > printerWidthInDots
            ? ClipToPrinterWidth(source, printerWidthInDots, effectiveJustification)
            : PadToPrinterWidth(source, printerWidthInDots, justification);
    }

    public static SKBitmap ClipToPrinterWidth(
        SKBitmap source,
        int printerWidthInDots,
        EscPosTextJustification? justification)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (printerWidthInDots <= 0 || source.Width <= printerWidthInDots)
        {
            return source.Copy();
        }

        return ClipToPrinterWidth(
            source,
            printerWidthInDots,
            justification.GetValueOrDefault(EscPosTextJustification.Left));
    }

    private static SKBitmap PadToPrinterWidth(
        SKBitmap source,
        int printerWidthInDots,
        EscPosTextJustification? justification)
    {
        if (justification is null)
        {
            return source.Copy();
        }

        var offsetX = justification switch
        {
            EscPosTextJustification.Center => (printerWidthInDots - source.Width) / 2,
            EscPosTextJustification.Right => printerWidthInDots - source.Width,
            _ => 0
        };

        var canvas = CreateTransparentBitmap(printerWidthInDots, source.Height);
        using var context = new SKCanvas(canvas);
        context.DrawBitmap(source, new SKPoint(offsetX, 0));
        return canvas;
    }

    private static SKBitmap ClipToPrinterWidth(
        SKBitmap source,
        int printerWidthInDots,
        EscPosTextJustification justification)
    {
        var sourceX = justification switch
        {
            EscPosTextJustification.Center => (source.Width - printerWidthInDots) / 2,
            EscPosTextJustification.Right => source.Width - printerWidthInDots,
            _ => 0
        };

        var result = CreateTransparentBitmap(printerWidthInDots, source.Height);
        using var context = new SKCanvas(result);
        var sourceRect = new SKRectI(sourceX, 0, sourceX + printerWidthInDots, source.Height);
        var targetRect = new SKRectI(0, 0, printerWidthInDots, source.Height);
        context.DrawBitmap(source, sourceRect, targetRect);
        return result;
    }

    private static SKBitmap CreateTransparentBitmap(int width, int height)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var context = new SKCanvas(bitmap);
        context.Clear(SKColors.Transparent);
        return bitmap;
    }
}
