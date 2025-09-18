namespace Printify.Tokenizer.Tests.EscPos;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

public sealed class EscPosTokenizerRasterTests
{
    private const byte ModeSingleDensity = 0x00;

    [Fact]
    public async Task EmitsRasterImageWithAllDotsSet()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();
        var store = context.BlobStorage;
        var payload = Enumerable.Repeat((byte)0xFF, 8).ToArray();
        var command = BuildRasterCommand(payload);

        session.Feed(command);
        session.Complete(CompletionReason.DataTimeout);

        var document = session.Document;
        var raster = Assert.IsType<RasterImage>(Assert.Single(document!.Elements));

        DocumentAssertions.Equal(
            document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new RasterImage(1, 8, 8, ModeSingleDensity, raster.BlobId, raster.ContentType, raster.ContentLength, raster.Checksum)
            });

        await using var blobStream = await store.GetAsync(raster.BlobId!);
        Assert.NotNull(blobStream);
        using var image = await Image.LoadAsync<L8>(blobStream!);
        Assert.Equal(8, image.Width);
        Assert.Equal(8, image.Height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                foreach (ref readonly var pixel in row)
                {
                    Assert.Equal(0, pixel.PackedValue);
                }
            }
        });
    }

    [Fact]
    public async Task EmitsRasterImageWithNoDotsSet()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();
        var store = context.BlobStorage;
        var payload = new byte[8];
        var command = BuildRasterCommand(payload);

        session.Feed(command);
        session.Complete(CompletionReason.DataTimeout);

        var document = session.Document;
        var raster = Assert.IsType<RasterImage>(Assert.Single(document!.Elements));

        DocumentAssertions.Equal(
            document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new RasterImage(1, 8, 8, ModeSingleDensity, raster.BlobId, raster.ContentType, raster.ContentLength, raster.Checksum)
            });

        await using var blobStream = await store.GetAsync(raster.BlobId!);
        Assert.NotNull(blobStream);
        using var image = await Image.LoadAsync<L8>(blobStream!);
        Assert.Equal(8, image.Width);
        Assert.Equal(8, image.Height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                foreach (ref readonly var pixel in row)
                {
                    Assert.Equal(byte.MaxValue, pixel.PackedValue);
                }
            }
        });
    }

    private static byte[] BuildRasterCommand(byte[] payload)
    {
        var command = new byte[8 + payload.Length];
        command[0] = EscPosTokenizer.Gs;
        command[1] = 0x76;
        command[2] = 0x30;
        command[3] = ModeSingleDensity;
        command[4] = 0x01; // xL (width bytes)
        command[5] = 0x00; // xH
        command[6] = 0x08; // yL (height)
        command[7] = 0x00; // yH
        Array.Copy(payload, 0, command, 8, payload.Length);
        return command;
    }
}
