using Xunit;
using EscPosCommands = Printify.Infrastructure.Printing.EscPos.Commands;
using EplCommands = Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Domain.Documents;
using Printify.Domain.Printing;
using Printify.Domain.Printers;
using Printify.Infrastructure.Mapping;
using Printify.Web.Contracts.Documents.Responses.Canvas;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;
using Printify.Web.Mapping;
using SkiaSharp;

namespace Printify.Tests.Shared.Document;

public static class DocumentAssertions
{
    public static void Equal(IReadOnlyList<Command> expectedElements, IReadOnlyList<Command> actualElements)
    {
        Assert.NotNull(expectedElements);
        try
        {
            Assert.Equal(expectedElements.Count, actualElements.Count);
        }
        catch (Exception e)
        {
            Console.WriteLine(e); //debugnow;
            Assert.Equal(expectedElements.Count, actualElements.Count);
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
                Console.WriteLine(e);
                throw;
            }

            switch (expected)
            {
                case EscPosCommands.AppendText expectedText:
                    var actualText = Assert.IsType<EscPosCommands.AppendText>(actualElement);
                    Assert.Equal(expectedText.TextBytes, actualText.TextBytes);
                    break;
                case EplCommands.EplScalableText expectedText:
                    var actualScalableText = Assert.IsType<EplCommands.EplScalableText>(actualElement);
                    Assert.Equal(expectedText.X, actualScalableText.X);
                    Assert.Equal(expectedText.Y, actualScalableText.Y);
                    Assert.Equal(expectedText.Rotation, actualScalableText.Rotation);
                    Assert.Equal(expectedText.Font, actualScalableText.Font);
                    Assert.Equal(expectedText.HorizontalMultiplication, actualScalableText.HorizontalMultiplication);
                    Assert.Equal(expectedText.VerticalMultiplication, actualScalableText.VerticalMultiplication);
                    Assert.Equal(expectedText.Reverse, actualScalableText.Reverse);
                    Assert.Equal(expectedText.TextBytes, actualScalableText.TextBytes);
                    break;
                case PrinterError:
                    _ = Assert.IsType<PrinterError>(actualElement);
                    break;
                case EscPosCommands.PrintBarcode expectedBarcode:
                    var actualBarcode = Assert.IsType<EscPosCommands.PrintBarcode>(actualElement);
                    Assert.Equal(expectedBarcode.Symbology, actualBarcode.Symbology);
                    Assert.True(actualBarcode.Width > 0);
                    Assert.True(actualBarcode.Height > 0);
                    Assert.NotNull(actualBarcode.Media);
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.ContentType));
                    Assert.True(actualBarcode.Media.Length > 0);
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.Sha256Checksum));
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.Url));
                    break;
                case EscPosCommands.PrintQrCode expectedQr:
                    var actualQr = Assert.IsType<EscPosCommands.PrintQrCode>(actualElement);
                    Assert.Equal(expectedQr.Data, actualQr.Data);
                    Assert.True(actualQr.Width > 0);
                    Assert.True(actualQr.Height > 0);
                    Assert.NotNull(actualQr.Media);
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.ContentType));
                    Assert.True(actualQr.Media.Length > 0);
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.Sha256Checksum));
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.Url));
                    break;
                case EplCommands.EplRasterImageUpload expectedRasterImageUpload:
                    var actualEplRasterImageUpload = Assert.IsType<EplCommands.EplRasterImageUpload>(actualElement);
                    Assert.Equal(expectedRasterImageUpload.X, actualEplRasterImageUpload.X);
                    Assert.Equal(expectedRasterImageUpload.Y, actualEplRasterImageUpload.Y);
                    Assert.Equal(expectedRasterImageUpload.Width, actualEplRasterImageUpload.Width);
                    Assert.Equal(expectedRasterImageUpload.Height, actualEplRasterImageUpload.Height);
                    Assert.Equal(expectedRasterImageUpload.MediaUpload.ContentType, actualEplRasterImageUpload.MediaUpload.ContentType);
                    Assert.True(actualEplRasterImageUpload.MediaUpload.Content.Length > 0);

                    // If expected media has content, verify pixels match
                    if (expectedRasterImageUpload.MediaUpload.Content.Length > 0)
                    {
                        AssertImagePixelsMatch(
                            expectedRasterImageUpload.MediaUpload.Content.ToArray(),
                            actualEplRasterImageUpload.MediaUpload.Content.ToArray(),
                            expectedRasterImageUpload.Width,
                            expectedRasterImageUpload.Height);
                    }
                    break;
                case EplCommands.EplRasterImage expectedRasterImage:
                    var actualEplRasterImage = Assert.IsType<EplCommands.EplRasterImage>(actualElement);
                    Assert.Equal(expectedRasterImage.X, actualEplRasterImage.X);
                    Assert.Equal(expectedRasterImage.Y, actualEplRasterImage.Y);
                    Assert.Equal(expectedRasterImage.Width, actualEplRasterImage.Width);
                    Assert.Equal(expectedRasterImage.Height, actualEplRasterImage.Height);
                    Assert.Equal(expectedRasterImage.Media.ContentType, actualEplRasterImage.Media.ContentType);
                    Assert.NotEmpty(actualEplRasterImage.Media.Sha256Checksum);
                    Assert.NotEmpty(actualEplRasterImage.Media.Url);
                    Assert.Equal(expectedRasterImage.Media.Length, actualEplRasterImage.Media.Length);
                    break;
                case EplCommands.EplPrintBarcodeUpload expectedBarcodeUpload:
                    var actualEplBarcodeUpload = Assert.IsType<EplCommands.EplPrintBarcodeUpload>(actualElement);
                    Assert.Equal(expectedBarcodeUpload.X, actualEplBarcodeUpload.X);
                    Assert.Equal(expectedBarcodeUpload.Y, actualEplBarcodeUpload.Y);
                    Assert.Equal(expectedBarcodeUpload.Rotation, actualEplBarcodeUpload.Rotation);
                    Assert.Equal(expectedBarcodeUpload.Type, actualEplBarcodeUpload.Type);
                    Assert.Equal(expectedBarcodeUpload.Width, actualEplBarcodeUpload.Width);
                    Assert.Equal(expectedBarcodeUpload.Height, actualEplBarcodeUpload.Height);
                    Assert.Equal(expectedBarcodeUpload.Hri, actualEplBarcodeUpload.Hri);
                    Assert.Equal(expectedBarcodeUpload.Data, actualEplBarcodeUpload.Data);
                    Assert.Equal(expectedBarcodeUpload.MediaUpload.ContentType, actualEplBarcodeUpload.MediaUpload.ContentType);
                    Assert.True(actualEplBarcodeUpload.MediaUpload.Content.Length > 0);
                    break;
                case EplCommands.EplPrintBarcode expectedBarcode:
                    var actualEplBarcode = Assert.IsType<EplCommands.EplPrintBarcode>(actualElement);
                    Assert.Equal(expectedBarcode.X, actualEplBarcode.X);
                    Assert.Equal(expectedBarcode.Y, actualEplBarcode.Y);
                    Assert.Equal(expectedBarcode.Rotation, actualEplBarcode.Rotation);
                    Assert.Equal(expectedBarcode.Type, actualEplBarcode.Type);
                    Assert.Equal(expectedBarcode.Width, actualEplBarcode.Width);
                    Assert.Equal(expectedBarcode.Height, actualEplBarcode.Height);
                    Assert.Equal(expectedBarcode.Hri, actualEplBarcode.Hri);
                    Assert.Equal(expectedBarcode.Data, actualEplBarcode.Data);
                    Assert.NotNull(actualEplBarcode.Media);
                    Assert.False(string.IsNullOrWhiteSpace(actualEplBarcode.Media.ContentType));
                    Assert.True(actualEplBarcode.Media.Length > 0);
                    Assert.False(string.IsNullOrWhiteSpace(actualEplBarcode.Media.Sha256Checksum));
                    Assert.False(string.IsNullOrWhiteSpace(actualEplBarcode.Media.Url));
                    break;
                case EplCommands.PrintBarcode expectedLegacyBarcode:
                    var actualLegacyBarcode = Assert.IsType<EplCommands.PrintBarcode>(actualElement);
                    Assert.Equal(expectedLegacyBarcode.X, actualLegacyBarcode.X);
                    Assert.Equal(expectedLegacyBarcode.Y, actualLegacyBarcode.Y);
                    Assert.Equal(expectedLegacyBarcode.Rotation, actualLegacyBarcode.Rotation);
                    Assert.Equal(expectedLegacyBarcode.Type, actualLegacyBarcode.Type);
                    Assert.Equal(expectedLegacyBarcode.Width, actualLegacyBarcode.Width);
                    Assert.Equal(expectedLegacyBarcode.Height, actualLegacyBarcode.Height);
                    Assert.Equal(expectedLegacyBarcode.Hri, actualLegacyBarcode.Hri);
                    Assert.Equal(expectedLegacyBarcode.Data, actualLegacyBarcode.Data);
                    break;
                case EscPosCommands.RasterImageUpload expectedRasterImageUpload:
                    var actualRasterImageUpload = Assert.IsType<EscPosCommands.RasterImageUpload>(actualElement);
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
                case EscPosCommands.RasterImage expectedRasterImage:
                    var actualRasterImage = Assert.IsType<EscPosCommands.RasterImage>(actualElement);
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
        IReadOnlyList<Command> expectedElements,
        Protocol expectedProtocol,
        Domain.Documents.Document? actual,
        int expectedWidthInDots,
        int? expectedHeightInDots)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedProtocol, actual.Protocol);
        Equal(expectedElements, actual.Commands.ToList());
    }

    public static void EqualCanvas(
        IReadOnlyList<IReadOnlyList<CanvasElementDto>> expectedCanvases,
        Protocol expectedProtocol,
        RenderedDocumentDto? actual,
        int expectedWidthInDots,
        int? expectedHeightInDots)
    {
        Assert.NotNull(actual);
        Assert.Equal(EnumMapper.ToString(expectedProtocol), actual.Protocol);
        Assert.NotNull(actual.Canvases);
        Assert.Equal(expectedCanvases.Count, actual.Canvases.Length);

        for (var canvasIndex = 0; canvasIndex < expectedCanvases.Count; canvasIndex++)
        {
            var expectedCanvas = expectedCanvases[canvasIndex];
            var actualCanvas = actual.Canvases[canvasIndex];

            Assert.Equal(expectedWidthInDots, actualCanvas.WidthInDots);
            Assert.Equal(expectedHeightInDots, actualCanvas.HeightInDots);

            EqualCanvasElements(expectedCanvas, actualCanvas.Items.ToList());

            // If there are any error debug elements, ErrorMessages must contain data
            var hasErrorDebugElement = actualCanvas.Items.Any(el =>
                el is CanvasDebugElementDto debug &&
                (debug.DebugType == "error" || debug.DebugType == "printerError"));

            if (hasErrorDebugElement)
            {
                Assert.NotNull(actual.ErrorMessages);
                Assert.NotEmpty(actual.ErrorMessages);
            }
        }
    }

    public static void EqualView(
        IReadOnlyList<CanvasElementDto> expectedElements,
        Protocol expectedProtocol,
        RenderedDocument? actual,
        int expectedWidthInDots,
        int? expectedHeightInDots)
    {
        Assert.NotNull(actual);
        var dto = RenderedDocumentMapper.ToCanvasResponseDto(actual);
        EqualCanvas([expectedElements], expectedProtocol, dto, expectedWidthInDots, expectedHeightInDots);
    }

    public static void EqualView(
        IReadOnlyList<IReadOnlyList<CanvasElementDto>> expectedCanvases,
        Protocol expectedProtocol,
        RenderedDocument? actual,
        int expectedWidthInDots,
        int? expectedHeightInDots)
    {
        Assert.NotNull(actual);
        var dto = RenderedDocumentMapper.ToCanvasResponseDto(actual);
        EqualCanvas(expectedCanvases, expectedProtocol, dto, expectedWidthInDots, expectedHeightInDots);
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

    public static void EqualBytes(int expectedBytesReceived, int expectedBytesSent, RenderedDocumentDto? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedBytesReceived, actual.BytesReceived);
        Assert.Equal(expectedBytesSent, actual.BytesSent);
    }

    public static void EqualCanvasElements(
        IReadOnlyList<CanvasElementDto> expectedCanvasElements,
        IReadOnlyList<CanvasElementDto> actualCanvasElements)
    {
        Assert.NotNull(expectedCanvasElements);
        try
        {
            Assert.Equal(expectedCanvasElements.Count, actualCanvasElements.Count);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        for (var index = 0; index < expectedCanvasElements.Count; index++)
        {
            var expected = expectedCanvasElements[index];
            var actualElement = actualCanvasElements[index];

            try
            {
                Assert.Equal(expected.GetType(), actualElement.GetType());

                // Debug output
                if (expected is CanvasTextElementDto expectedText)
                {
                    var actualText = actualElement as CanvasTextElementDto;
                    Console.WriteLine($"DEBUG: Expected Text='{expectedText.Text}', Actual Text='{actualText?.Text}'");
                }

                if (expected is CanvasDebugElementDto || actualElement is CanvasDebugElementDto)
                {
                    Assert.Equal(expected.LengthInBytes, actualElement.LengthInBytes);

                    // Verify CommandDescription is not empty for debug elements
                    if (actualElement is CanvasDebugElementDto actualDebug)
                    {
                        Assert.NotNull(actualDebug.CommandDescription);
                        Assert.NotEmpty(actualDebug.CommandDescription);
                        Assert.All(actualDebug.CommandDescription, line =>
                        {
                            Assert.False(string.IsNullOrWhiteSpace(line),
                                $"CommandDescription for {actualDebug.DebugType} contains empty/whitespace line");
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e); //todo debugnow
                throw;
            }

            switch (expected)
            {
                case CanvasTextElementDto expectedText:
                    var actualText = Assert.IsType<CanvasTextElementDto>(actualElement);
                    Assert.Equal(expectedText.Text, actualText.Text);
                    Assert.Equal(expectedText.X, actualText.X);
                    Assert.Equal(expectedText.Y, actualText.Y);
                    Assert.Equal(expectedText.Width, actualText.Width);
                    Assert.Equal(expectedText.Height, actualText.Height);
                    Assert.Equal(expectedText.FontName, actualText.FontName);
                    Assert.Equal(expectedText.CharSpacing, actualText.CharSpacing);
                    Assert.Equal(expectedText.IsBold, actualText.IsBold);
                    Assert.Equal(expectedText.IsUnderline, actualText.IsUnderline);
                    Assert.Equal(expectedText.IsReverse, actualText.IsReverse);
                    Assert.Equal(expectedText.CharScaleX, actualText.CharScaleX);
                    Assert.Equal(expectedText.CharScaleY, actualText.CharScaleY);
                    Assert.Equal(expectedText.Rotation, actualText.Rotation);
                    break;
                case CanvasImageElementDto expectedImage:
                    var actualImage = Assert.IsType<CanvasImageElementDto>(actualElement);
                    Assert.Equal(expectedImage.X, actualImage.X);
                    Assert.Equal(expectedImage.Y, actualImage.Y);
                    Assert.Equal(expectedImage.Width, actualImage.Width);
                    Assert.Equal(expectedImage.Height, actualImage.Height);
                    Assert.NotNull(actualImage.Media);
                    Assert.True(actualImage.Media.Size > 0);
                    Assert.Equal(expectedImage.Media.ContentType, actualImage.Media.ContentType);
                    Assert.Equal(expectedImage.Rotation, actualImage.Rotation);
                    break;
                case CanvasDebugElementDto expectedDebug:
                    var actualDebug = Assert.IsType<CanvasDebugElementDto>(actualElement);
                    Assert.Equal(expectedDebug.DebugType, actualDebug.DebugType);
                    break;
                default:
                    Assert.Equal(NormalizeViewElement(expected, actualElement), actualElement);
                    break;
            }
        }
    }

    private static CanvasElementDto NormalizeViewElement(CanvasElementDto expected, CanvasElementDto actual)
    {
        return expected with
        {
            CommandRaw = actual.CommandRaw,
            CommandDescription = actual.CommandDescription
        };
    }

    private static Command NormalizeDomain(Command expected, Command actual)
    {
        return expected with
        {
            RawBytes = actual.RawBytes,
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
