using SkiaSharp;

using Printify.Domain.Media;
using Printify.Infrastructure.Media;

namespace Printify.Infrastructure.Tests.Media;

public sealed class MediaServiceRasterImageTests
{
    [Fact]
    public void ConvertToMediaUpload_PreservesRasterImageDimensions()
    {
        var service = new MediaService();
        var bitmap = new MonochromeBitmap(
            width: 10,
            height: 2,
            data:
            [
                0b1000_0001,
                0b1000_0000,
                0b0100_0000,
                0b0000_0000
            ]);

        var upload = service.ConvertToMediaUpload(bitmap);
        using var decoded = SKBitmap.Decode(upload.Content.ToArray());

        Assert.Equal("image/png", upload.ContentType);
        Assert.NotNull(decoded);
        Assert.Equal(bitmap.Width, decoded.Width);
        Assert.Equal(bitmap.Height, decoded.Height);
    }

    [Fact]
    public void ConvertToMediaUpload_KeepsOneBitsBlackAndZeroBitsTransparent()
    {
        var service = new MediaService();
        var bitmap = new MonochromeBitmap(
            width: 10,
            height: 2,
            data:
            [
                0b1000_0001,
                0b1000_0000,
                0b0100_0000,
                0b0000_0000
            ]);

        var upload = service.ConvertToMediaUpload(bitmap);
        using var decoded = SKBitmap.Decode(upload.Content.ToArray());

        Assert.NotNull(decoded);
        Assert.Equal(SKColors.Black, decoded.GetPixel(0, 0));
        Assert.Equal(SKColors.Black, decoded.GetPixel(7, 0));
        Assert.Equal(SKColors.Black, decoded.GetPixel(8, 0));
        Assert.Equal(SKColors.Black, decoded.GetPixel(1, 1));
        Assert.Equal(0, decoded.GetPixel(1, 0).Alpha);
        Assert.Equal(0, decoded.GetPixel(9, 1).Alpha);
    }
}
