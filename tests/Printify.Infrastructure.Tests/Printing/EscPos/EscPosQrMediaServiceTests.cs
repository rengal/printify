using CodeGlyphX;
using SkiaSharp;

using Printify.Infrastructure.Media;
using Printify.Infrastructure.Printing.EscPos;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class EscPosQrMediaServiceTests
{
    private const int QuietZoneInModules = 4;

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

        // ESC/POS QR layout includes the encoded modules plus the configured quiet zone on all four sides.
        return (qrCode.Size + QuietZoneInModules * 2) * moduleSizeInDots;
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
