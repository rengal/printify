using SkiaSharp;

using Printify.Infrastructure.Media;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Tests.Media;

public sealed class EscPosRasterImageFitterTests
{
    [Fact]
    public void FitToPrinterWidth_ReturnsNaturalWidth_WhenImageIsNarrowerAndJustificationIsNotSet()
    {
        using var source = CreateHorizontalColorStrip(width: 3);

        using var result = EscPosRasterImageFitter.FitToPrinterWidth(
            source,
            printerWidthInDots: 5,
            justification: null);

        Assert.Equal(3, result.Width);
        Assert.Equal(0, result.GetPixel(0, 0).Red);
        Assert.Equal(2, result.GetPixel(2, 0).Red);
    }

    [Fact]
    public void FitToPrinterWidth_PadsImage_WhenImageIsNarrowerAndJustificationIsSet()
    {
        using var source = CreateHorizontalColorStrip(width: 3);

        using var result = EscPosRasterImageFitter.FitToPrinterWidth(
            source,
            printerWidthInDots: 7,
            justification: EscPosTextJustification.Center);

        Assert.Equal(7, result.Width);
        Assert.Equal(0, result.GetPixel(0, 0).Alpha);
        Assert.Equal(0, result.GetPixel(1, 0).Alpha);
        Assert.Equal(0, result.GetPixel(2, 0).Red);
        Assert.Equal(2, result.GetPixel(4, 0).Red);
        Assert.Equal(0, result.GetPixel(6, 0).Alpha);
    }

    [Theory]
    [InlineData(EscPosTextJustification.Left, 0, 1, 2, 3)]
    [InlineData(EscPosTextJustification.Center, 1, 2, 3, 4)]
    [InlineData(EscPosTextJustification.Right, 2, 3, 4, 5)]
    public void FitToPrinterWidth_ClipsImage_WhenImageIsWider(
        EscPosTextJustification justification,
        int firstRed,
        int secondRed,
        int thirdRed,
        int fourthRed)
    {
        using var source = CreateHorizontalColorStrip(width: 6);

        using var result = EscPosRasterImageFitter.FitToPrinterWidth(
            source,
            printerWidthInDots: 4,
            justification: justification);

        Assert.Equal(4, result.Width);
        Assert.Equal(firstRed, result.GetPixel(0, 0).Red);
        Assert.Equal(secondRed, result.GetPixel(1, 0).Red);
        Assert.Equal(thirdRed, result.GetPixel(2, 0).Red);
        Assert.Equal(fourthRed, result.GetPixel(3, 0).Red);
    }

    private static SKBitmap CreateHorizontalColorStrip(int width)
    {
        var bitmap = new SKBitmap(width, 1, SKColorType.Bgra8888, SKAlphaType.Premul);

        for (var x = 0; x < width; x++)
        {
            bitmap.SetPixel(x, 0, new SKColor((byte)x, 0, 0, 255));
        }

        return bitmap;
    }
}
