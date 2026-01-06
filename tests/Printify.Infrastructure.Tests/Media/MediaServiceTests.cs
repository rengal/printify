using Printify.Domain.Media;
using Printify.Infrastructure.Media;
using SkiaSharp;
using Xunit;

namespace Printify.Infrastructure.Tests.Media;

public sealed class MediaServiceTests
{
    private readonly MediaService _mediaService = new();

    [Fact]
    public void ConvertToMediaUpload_SetBitsAreBlack_UnsetBitsAreTransparent()
    {
        // Arrange: Create a 3x2 bitmap with specific pattern
        // Row 1: 11100000 (bits 7,6,5 set = first 3 pixels black)
        // Row 2: 00011000 (bits 4,3 set = pixels 4,5 black)
        var data = new byte[]
        {
            0b11100000, // Row 1: XXX_____ (X=black, _=transparent)
            0b00011000  // Row 2: ___XX___ (X=black, _=transparent)
        };
        var bitmap = new MonochromeBitmap(width: 8, height: 2, data);

        // Act
        var result = _mediaService.ConvertToMediaUpload(bitmap);

        // Assert
        Assert.Equal("image/png", result.ContentType);
        Assert.True(result.Content.Length > 0);

        // Decode PNG and verify pixels
        using var image = SKBitmap.Decode(result.Content.ToArray());
        Assert.NotNull(image);
        Assert.Equal(8, image!.Width);
        Assert.Equal(2, image.Height);

        // Row 0: First 3 pixels should be black (opaque), rest transparent
        AssertBlackOpaque(image.GetPixel(0, 0), x: 0, y: 0);
        AssertBlackOpaque(image.GetPixel(1, 0), x: 1, y: 0);
        AssertBlackOpaque(image.GetPixel(2, 0), x: 2, y: 0);
        AssertTransparent(image.GetPixel(3, 0), x: 3, y: 0);
        AssertTransparent(image.GetPixel(4, 0), x: 4, y: 0);
        AssertTransparent(image.GetPixel(5, 0), x: 5, y: 0);
        AssertTransparent(image.GetPixel(6, 0), x: 6, y: 0);
        AssertTransparent(image.GetPixel(7, 0), x: 7, y: 0);

        // Row 1: Pixels 3,4 should be black (opaque), rest transparent
        AssertTransparent(image.GetPixel(0, 1), x: 0, y: 1);
        AssertTransparent(image.GetPixel(1, 1), x: 1, y: 1);
        AssertTransparent(image.GetPixel(2, 1), x: 2, y: 1);
        AssertBlackOpaque(image.GetPixel(3, 1), x: 3, y: 1);
        AssertBlackOpaque(image.GetPixel(4, 1), x: 4, y: 1);
        AssertTransparent(image.GetPixel(5, 1), x: 5, y: 1);
        AssertTransparent(image.GetPixel(6, 1), x: 6, y: 1);
        AssertTransparent(image.GetPixel(7, 1), x: 7, y: 1);
    }

    [Fact]
    public void ConvertToMediaUpload_AllBitsSet_AllPixelsBlack()
    {
        // Arrange: 2x2 bitmap, all bits set
        var data = new byte[] { 0xFF, 0xFF };
        var bitmap = new MonochromeBitmap(width: 8, height: 2, data);

        // Act
        var result = _mediaService.ConvertToMediaUpload(bitmap);

        // Assert
        using var image = SKBitmap.Decode(result.Content.ToArray());
        Assert.NotNull(image);

        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                AssertBlackOpaque(image!.GetPixel(x, y), x, y);
            }
        }
    }

    [Fact]
    public void ConvertToMediaUpload_AllBitsUnset_AllPixelsTransparent()
    {
        // Arrange: 2x2 bitmap, all bits unset (zero initialized)
        var data = new byte[] { 0x00, 0x00 };
        var bitmap = new MonochromeBitmap(width: 8, height: 2, data);

        // Act
        var result = _mediaService.ConvertToMediaUpload(bitmap);

        // Assert
        using var image = SKBitmap.Decode(result.Content.ToArray());
        Assert.NotNull(image);

        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                AssertTransparent(image!.GetPixel(x, y), x, y);
            }
        }
    }

    [Fact]
    public void ConvertToMediaUpload_CheckerboardPattern_AlternatingPixels()
    {
        // Arrange: 8x2 checkerboard pattern
        // Row 1: 10101010
        // Row 2: 01010101
        var data = new byte[]
        {
            0b10101010, // Alternating: X_X_X_X_
            0b01010101  // Alternating: _X_X_X_X
        };
        var bitmap = new MonochromeBitmap(width: 8, height: 2, data);

        // Act
        var result = _mediaService.ConvertToMediaUpload(bitmap);

        // Assert
        using var image = SKBitmap.Decode(result.Content.ToArray());
        Assert.NotNull(image);

        // Row 0: alternating black/transparent starting with black
        for (int x = 0; x < 8; x++)
        {
            if (x % 2 == 0)
                AssertBlackOpaque(image!.GetPixel(x, 0), x, 0);
            else
                AssertTransparent(image!.GetPixel(x, 0), x, 0);
        }

        // Row 1: alternating transparent/black starting with transparent
        for (int x = 0; x < 8; x++)
        {
            if (x % 2 == 0)
                AssertTransparent(image!.GetPixel(x, 1), x, 1);
            else
                AssertBlackOpaque(image!.GetPixel(x, 1), x, 1);
        }
    }

    private static void AssertBlackOpaque(SKColor pixel, int x, int y)
    {
        Assert.True(
            pixel.Red == 0 && pixel.Green == 0 && pixel.Blue == 0 && pixel.Alpha == 255,
            $"Expected black opaque pixel at ({x},{y}) but got R={pixel.Red}, G={pixel.Green}, B={pixel.Blue}, A={pixel.Alpha}");
    }

    private static void AssertTransparent(SKColor pixel, int x, int y)
    {
        Assert.True(
            pixel.Alpha == 0,
            $"Expected transparent pixel at ({x},{y}) but got R={pixel.Red}, G={pixel.Green}, B={pixel.Blue}, A={pixel.Alpha}");
    }
}
