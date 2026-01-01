using Printify.Domain.Media;
using Printify.Infrastructure.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
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
        using var image = Image.Load<Rgba32>(result.Content.ToArray());
        Assert.Equal(8, image.Width);
        Assert.Equal(2, image.Height);

        // Row 0: First 3 pixels should be black (opaque), rest transparent
        var row0 = image.DangerousGetPixelRowMemory(0).Span;
        AssertBlackOpaque(row0[0], x: 0, y: 0);
        AssertBlackOpaque(row0[1], x: 1, y: 0);
        AssertBlackOpaque(row0[2], x: 2, y: 0);
        AssertTransparent(row0[3], x: 3, y: 0);
        AssertTransparent(row0[4], x: 4, y: 0);
        AssertTransparent(row0[5], x: 5, y: 0);
        AssertTransparent(row0[6], x: 6, y: 0);
        AssertTransparent(row0[7], x: 7, y: 0);

        // Row 1: Pixels 3,4 should be black (opaque), rest transparent
        var row1 = image.DangerousGetPixelRowMemory(1).Span;
        AssertTransparent(row1[0], x: 0, y: 1);
        AssertTransparent(row1[1], x: 1, y: 1);
        AssertTransparent(row1[2], x: 2, y: 1);
        AssertBlackOpaque(row1[3], x: 3, y: 1);
        AssertBlackOpaque(row1[4], x: 4, y: 1);
        AssertTransparent(row1[5], x: 5, y: 1);
        AssertTransparent(row1[6], x: 6, y: 1);
        AssertTransparent(row1[7], x: 7, y: 1);
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
        using var image = Image.Load<Rgba32>(result.Content.ToArray());

        for (int y = 0; y < 2; y++)
        {
            var row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < 8; x++)
            {
                AssertBlackOpaque(row[x], x, y);
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
        using var image = Image.Load<Rgba32>(result.Content.ToArray());

        for (int y = 0; y < 2; y++)
        {
            var row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < 8; x++)
            {
                AssertTransparent(row[x], x, y);
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
        using var image = Image.Load<Rgba32>(result.Content.ToArray());

        // Row 0: alternating black/transparent starting with black
        var row0 = image.DangerousGetPixelRowMemory(0).Span;
        for (int x = 0; x < 8; x++)
        {
            if (x % 2 == 0)
                AssertBlackOpaque(row0[x], x, 0);
            else
                AssertTransparent(row0[x], x, 0);
        }

        // Row 1: alternating transparent/black starting with transparent
        var row1 = image.DangerousGetPixelRowMemory(1).Span;
        for (int x = 0; x < 8; x++)
        {
            if (x % 2 == 0)
                AssertTransparent(row1[x], x, 1);
            else
                AssertBlackOpaque(row1[x], x, 1);
        }
    }

    private static void AssertBlackOpaque(Rgba32 pixel, int x, int y)
    {
        Assert.True(
            pixel.R == 0 && pixel.G == 0 && pixel.B == 0 && pixel.A == 255,
            $"Expected black opaque pixel at ({x},{y}) but got R={pixel.R}, G={pixel.G}, B={pixel.B}, A={pixel.A}");
    }

    private static void AssertTransparent(Rgba32 pixel, int x, int y)
    {
        Assert.True(
            pixel.A == 0,
            $"Expected transparent pixel at ({x},{y}) but got R={pixel.R}, G={pixel.G}, B={pixel.B}, A={pixel.A}");
    }
}
