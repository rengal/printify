using Xunit;
using DomainElements = Printify.Domain.Documents.Elements;
using EscPosElements = Printify.Domain.Documents.Elements.EscPos;
using Printify.Domain.Mapping;
using Printify.Domain.Printers;
using Printify.Domain.Documents.View;
using Printify.Web.Contracts.Documents.Responses.View;
using Printify.Web.Contracts.Documents.Responses.View.Elements;
using Printify.Web.Mapping;
using SkiaSharp;

namespace Printify.Tests.Shared.Document;

public static class DocumentAssertions
{
    public static void Equal(IReadOnlyList<DomainElements.Element> expectedElements, IReadOnlyList<DomainElements.Element> actualElements)
    {
        Assert.NotNull(expectedElements);
        Assert.Equal(expectedElements.Count, actualElements.Count);

        for (var index = 0; index < expectedElements.Count; index++)
        {
            var expected = expectedElements[index];
            var actualElement = actualElements[index];

            try
            {
                Assert.Equal(expected.GetType(), actualElement.GetType());
                Assert.Equal(expected.LengthInBytes, actualElement.LengthInBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            switch (expected)
            {
                case DomainElements.PrinterError:
                    _ = Assert.IsType<DomainElements.PrinterError>(actualElement);
                    break;
                case EscPosElements.PrintBarcode expectedBarcode:
                    var actualBarcode = Assert.IsType<EscPosElements.PrintBarcode>(actualElement);
                    Assert.Equal(expectedBarcode.Symbology, actualBarcode.Symbology);
                    Assert.True(actualBarcode.Width > 0);
                    Assert.True(actualBarcode.Height > 0);
                    Assert.NotNull(actualBarcode.Media);
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.ContentType));
                    Assert.True(actualBarcode.Media.Length > 0);
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.Sha256Checksum));
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.Url));
                    break;
                case EscPosElements.PrintQrCode expectedQr:
                    var actualQr = Assert.IsType<EscPosElements.PrintQrCode>(actualElement);
                    Assert.Equal(expectedQr.Data, actualQr.Data);
                    Assert.True(actualQr.Width > 0);
                    Assert.True(actualQr.Height > 0);
                    Assert.NotNull(actualQr.Media);
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.ContentType));
                    Assert.True(actualQr.Media.Length > 0);
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.Sha256Checksum));
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.Url));
                    break;
                case EscPosElements.RasterImageUpload expectedRasterImageUpload:
                    var actualRasterImageUpload = Assert.IsType<EscPosElements.RasterImageUpload>(actualElement);
                    Assert.Equal(expectedRasterImageUpload.Width, actualRasterImageUpload.Width);
                    Assert.Equal(expectedRasterImageUpload.Height, actualRasterImageUpload.Height);
                    Assert.Equal(expectedRasterImageUpload.Media.ContentType, actualRasterImageUpload.Media.ContentType);
                    Assert.True(actualRasterImageUpload.Media.Content.Length > 0);

                    // If expected media has content, verify pixels match
                    if (expectedRasterImageUpload.Media.Content.Length > 0)
                    {
                        AssertImagePixelsMatch(
                            expectedRasterImageUpload.Media.Content.ToArray(),
                            actualRasterImageUpload.Media.Content.ToArray(),
                            expectedRasterImageUpload.Width,
                            expectedRasterImageUpload.Height);
                    }
                    break;
                case EscPosElements.RasterImage expectedRasterImage:
                    var actualRasterImage = Assert.IsType<EscPosElements.RasterImage>(actualElement);
                    Assert.Equal(expectedRasterImage.Width, actualRasterImage.Width);
                    Assert.Equal(expectedRasterImage.Height, actualRasterImage.Height);
                    Assert.Equal(expectedRasterImage.Media.ContentType, actualRasterImage.Media.ContentType);
                    Assert.NotEmpty(actualRasterImage.Media.Sha256Checksum);
                    Assert.NotEmpty(actualRasterImage.Media.Url);
                    Assert.Equal(expectedRasterImage.Media.Length, actualRasterImage.Media.Length);
                    break;
                default:
                    Assert.Equal(NormalizeDomain(expected, actualElement), actualElement);
                    break;
            }
        }
    }

    public static void Equal(
        IReadOnlyList<DomainElements.Element> expectedElements,
        Protocol expectedProtocol,
        Domain.Documents.Document? actual,
        int expectedWidthInDots,
        int? expectedHeightInDots)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedProtocol, actual.Protocol);
        Assert.Equal(expectedWidthInDots, actual.WidthInDots);
        Assert.Equal(expectedHeightInDots, actual.HeightInDots);

        Equal(expectedElements, actual.Elements.ToList());
    }

    public static void EqualView(
        IReadOnlyList<ViewElementDto> expectedElements,
        Protocol expectedProtocol,
        ViewDocumentDto? actual,
        int expectedWidthInDots,
        int? expectedHeightInDots)
    {
        Assert.NotNull(actual);
        Assert.Equal(DomainMapper.ToString(expectedProtocol), actual.Protocol);
        Assert.Equal(expectedWidthInDots, actual.WidthInDots);
        Assert.Equal(expectedHeightInDots, actual.HeightInDots);

        EqualViewElements(expectedElements, actual.Elements.ToList());
    }

    public static void EqualView(
        IReadOnlyList<ViewElementDto> expectedElements,
        Protocol expectedProtocol,
        ViewDocument? actual,
        int expectedWidthInDots,
        int? expectedHeightInDots)
    {
        Assert.NotNull(actual);
        var dto = ViewDocumentMapper.ToViewResponseDto(actual);
        EqualView(expectedElements, expectedProtocol, dto, expectedWidthInDots, expectedHeightInDots);
    }

    public static void EqualBytes(
        int expectedBytesReceived,
        int expectedBytesSent,
        Domain.Documents.Document? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedBytesReceived, actual.BytesReceived);
        Assert.Equal(expectedBytesSent, actual.BytesSent);
    }

    public static void EqualBytes(int expectedBytesReceived, int expectedBytesSent, ViewDocumentDto? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedBytesReceived, actual.BytesReceived);
        Assert.Equal(expectedBytesSent, actual.BytesSent);
    }

    public static void EqualViewElements(
        IReadOnlyList<ViewElementDto> expectedElements,
        IReadOnlyList<ViewElementDto> actualElements)
    {
        Assert.NotNull(expectedElements);
        try
        {
            Assert.Equal(expectedElements.Count, actualElements.Count);
        }
        catch (Exception e)
        {
            Console.WriteLine(e); //todo debugnow
            throw;
        }

        for (var index = 0; index < expectedElements.Count; index++)
        {
            var expected = expectedElements[index];
            var actualElement = actualElements[index];

            try
            {
                Assert.Equal(expected.GetType(), actualElement.GetType());
                Assert.Equal(expected.LengthInBytes, actualElement.LengthInBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e); //todo debugnow
                throw;
            }

            switch (expected)
            {
                case ViewTextElementDto expectedText:
                    var actualText = Assert.IsType<ViewTextElementDto>(actualElement);
                    Assert.Equal(expectedText.Text, actualText.Text);
                    Assert.Equal(expectedText.X, actualText.X);
                    Assert.Equal(expectedText.Y, actualText.Y);
                    Assert.Equal(expectedText.Width, actualText.Width);
                    Assert.Equal(expectedText.Height, actualText.Height);
                    Assert.Equal(expectedText.Font, actualText.Font);
                    Assert.Equal(expectedText.CharSpacing, actualText.CharSpacing);
                    Assert.Equal(expectedText.IsBold, actualText.IsBold);
                    Assert.Equal(expectedText.IsUnderline, actualText.IsUnderline);
                    Assert.Equal(expectedText.IsReverse, actualText.IsReverse);
                    Assert.Equal(expectedText.CharScaleX, actualText.CharScaleX);
                    Assert.Equal(expectedText.CharScaleY, actualText.CharScaleY);
                    break;
                case ViewImageElementDto expectedImage:
                    var actualImage = Assert.IsType<ViewImageElementDto>(actualElement);
                    Assert.Equal(expectedImage.X, actualImage.X);
                    Assert.Equal(expectedImage.Y, actualImage.Y);
                    Assert.Equal(expectedImage.Width, actualImage.Width);
                    Assert.Equal(expectedImage.Height, actualImage.Height);
                    Assert.NotNull(actualImage.Media);
                    Assert.True(actualImage.Media.Length > 0);
                    Assert.Equal(expectedImage.Media.ContentType, actualImage.Media.ContentType);
                    break;
                case ViewDebugElementDto expectedDebug:
                    var actualDebug = Assert.IsType<ViewDebugElementDto>(actualElement);
                    Assert.Equal(expectedDebug.DebugType, actualDebug.DebugType);
                    break;
                default:
                    Assert.Equal(NormalizeViewElement(expected, actualElement), actualElement);
                    break;
            }
        }
    }

    private static ViewElementDto NormalizeViewElement(ViewElementDto expected, ViewElementDto actual)
    {
        return expected with
        {
            CommandRaw = actual.CommandRaw,
            CommandDescription = actual.CommandDescription,
            ZIndex = actual.ZIndex
        };
    }

    private static DomainElements.Element NormalizeDomain(DomainElements.Element expected, DomainElements.Element actual)
    {
        return expected with
        {
            CommandRaw = actual.CommandRaw,
            LengthInBytes = actual.LengthInBytes
        };
    }

    /// <summary>
    /// Verifies that two images have matching pixels at the binary level:
    /// - Colored pixels (alpha > 0) must match between expected and actual
    /// - Transparent pixels (alpha == 0) must match between expected and actual
    /// Format-agnostic: works with any image format SkiaSharp can decode.
    /// </summary>
    private static void AssertImagePixelsMatch(
        byte[] expectedImageData,
        byte[] actualImageData,
        int expectedWidth,
        int expectedHeight)
    {
        using var expectedImage = SKBitmap.Decode(expectedImageData);
        using var actualImage = SKBitmap.Decode(actualImageData);

        Assert.NotNull(expectedImage);
        Assert.NotNull(actualImage);
        Assert.Equal(expectedWidth, expectedImage!.Width);
        Assert.Equal(expectedHeight, expectedImage.Height);
        Assert.Equal(expectedWidth, actualImage!.Width);
        Assert.Equal(expectedHeight, actualImage.Height);

        // Verify pixels match at binary level (colored vs transparent)
        for (int y = 0; y < expectedHeight; y++)
        {
            for (int x = 0; x < expectedWidth; x++)
            {
                var expectedPixel = expectedImage.GetPixel(x, y);
                var actualPixel = actualImage.GetPixel(x, y);

                // Binary check: both transparent OR both colored
                var expectedIsTransparent = expectedPixel.Alpha == 0;
                var actualIsTransparent = actualPixel.Alpha == 0;

                Assert.True(
                    expectedIsTransparent == actualIsTransparent,
                    $"Pixel at ({x},{y}) mismatch: expected {(expectedIsTransparent ? "transparent" : "colored")}, " +
                    $"got {(actualIsTransparent ? "transparent" : "colored")} " +
                    $"(expected A={expectedPixel.Alpha}, actual A={actualPixel.Alpha})");
            }
        }
    }
}
