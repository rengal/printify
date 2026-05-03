using CodeGlyphX;
using SkiaSharp;

using Printify.Infrastructure.Media;
using Printify.Infrastructure.Printing.EscPos;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class EscPosQrMediaServiceTests
{
    public static TheoryData<string, int, EscPosQrErrorCorrectionLevel> QrSizeCases => new()
    {
        { "A", 2, EscPosQrErrorCorrectionLevel.Low },
        { "https://google.com", 3, EscPosQrErrorCorrectionLevel.Low },
        { "Order #12345; total=999.99; terminal=POS-01", 4, EscPosQrErrorCorrectionLevel.Medium },
        { new string('X', 120), 5, EscPosQrErrorCorrectionLevel.Quartile },
        {
            "\u041a\u0438\u0440\u0438\u043b\u043b\u0438\u0446\u0430 UTF-8 payload",
            6,
            EscPosQrErrorCorrectionLevel.High
        },
        { new string('9', 300), 8, EscPosQrErrorCorrectionLevel.Medium }
    };

    [Theory]
    [MemberData(nameof(QrSizeCases))]
    public void GenerateQrMedia_UsesModuleSizeAsPixelUnit(
        string data,
        int moduleSizeInDots,
        EscPosQrErrorCorrectionLevel correctionLevel)
    {
        var service = new MediaService();
        var options = new QrRenderOptions(
            data,
            EscPosQrModel.Model2,
            moduleSizeInDots,
            correctionLevel,
            Justification: null,
            PrinterWidthInDots: null);

        var result = service.GenerateQrMedia(options);
        var expectedSide = CalculateExpectedSideInDots(data, moduleSizeInDots, correctionLevel);
        var toleranceInDots = moduleSizeInDots;

        Assert.Equal(result.Width, result.Height);
        Assert.Equal(0, result.Width % moduleSizeInDots);
        Assert.InRange(result.Width, expectedSide - toleranceInDots, expectedSide + toleranceInDots);
    }

    [Theory]
    [MemberData(nameof(QrSizeCases))]
    public void GenerateQrMedia_ReturnsPngWithReportedDimensions(
        string data,
        int moduleSizeInDots,
        EscPosQrErrorCorrectionLevel correctionLevel)
    {
        var service = new MediaService();
        var options = new QrRenderOptions(
            data,
            EscPosQrModel.Model2,
            moduleSizeInDots,
            correctionLevel,
            Justification: null,
            PrinterWidthInDots: null);

        var result = service.GenerateQrMedia(options);
        using var bitmap = SKBitmap.Decode(result.Media.Content.ToArray());

        Assert.Equal("image/png", result.Media.ContentType);
        Assert.NotNull(bitmap);
        Assert.Equal(result.Width, bitmap.Width);
        Assert.Equal(result.Height, bitmap.Height);
    }

    [Theory]
    [InlineData(EscPosTextJustification.Left)]
    [InlineData(EscPosTextJustification.Center)]
    [InlineData(EscPosTextJustification.Right)]
    public void GenerateQrMedia_ClipsToPrinterWidth_WhenQrImageIsWiderThanPrinter(
        EscPosTextJustification justification)
    {
        const int printerWidthInDots = 120;
        var service = new MediaService();
        var options = new QrRenderOptions(
            new string('X', 300),
            EscPosQrModel.Model2,
            ModuleSizeInDots: 8,
            ErrorCorrectionLevel: EscPosQrErrorCorrectionLevel.High,
            justification,
            printerWidthInDots);

        var result = service.GenerateQrMedia(options);
        using var bitmap = SKBitmap.Decode(result.Media.Content.ToArray());

        Assert.Equal(printerWidthInDots, result.Width);
        Assert.NotNull(bitmap);
        Assert.Equal(result.Width, bitmap.Width);
        Assert.Equal(result.Height, bitmap.Height);
    }

    [Theory]
    [InlineData(EscPosTextJustification.Center)]
    [InlineData(EscPosTextJustification.Right)]
    public void GenerateQrMedia_DoesNotPadToPrinterWidth_WhenQrImageIsNarrowerThanPrinter(
        EscPosTextJustification justification)
    {
        const string data = "A";
        const int moduleSizeInDots = 4;
        var service = new MediaService();
        var options = new QrRenderOptions(
            data,
            EscPosQrModel.Model2,
            moduleSizeInDots,
            EscPosQrErrorCorrectionLevel.Low,
            justification,
            PrinterWidthInDots: 512);

        var result = service.GenerateQrMedia(options);
        var expectedSide = CalculateExpectedSideInDots(
            data,
            moduleSizeInDots,
            EscPosQrErrorCorrectionLevel.Low);

        Assert.Equal(expectedSide, result.Width);
        Assert.Equal(expectedSide, result.Height);
    }

    private static int CalculateExpectedSideInDots(
        string data,
        int moduleSizeInDots,
        EscPosQrErrorCorrectionLevel correctionLevel)
    {
        var options = new QrEasyOptions
        {
            ErrorCorrectionLevel = MapErrorCorrection(correctionLevel),
            TextEncoding = QrTextEncoding.Utf8,
            IncludeEci = true
        };

        var qrCode = QrCode.Encode(data, options);

        // QR media is a pure symbol; ESC/POS quiet-zone spacing is applied later by the renderer.
        return qrCode.Size * moduleSizeInDots;
    }

    private static QrErrorCorrectionLevel MapErrorCorrection(EscPosQrErrorCorrectionLevel level)
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
