using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Services.Tokenizer;
using Printify.TestServices;

namespace Printify.Tokenizer.Tests.EscPos;

public sealed class RasterTests
{
    private const byte ModeSingleDensity = 0x00;

    [Fact]
    public async Task EmitsRasterImageWithAllDotsSet()
    {
        await using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        
        Assert.NotNull(context.Tokenizer);

        var session = context.Tokenizer.CreateSession();
        var payload = Enumerable.Repeat((byte)0xFF, 8).ToArray();
        var command = BuildRasterCommand(payload);

        session.Feed(command);
        session.Complete(CompletionReason.DataTimeout);

        var document = session.Document;
        Assert.NotNull(document);
        Assert.NotNull(document.Elements);

        DocumentAssertions.Equal(
            document,
            Protocol.EscPos,
            expectedElements:
            [
                new RasterImageContent(1, 8, 8, new MediaContent(new MediaMeta("image/png", 12, null), null))
            ]);
    }

    [Fact]
    public async Task EmitsRasterImageWithNoDotsSet()
    {
        await using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        
        Assert.NotNull(context.Tokenizer);

        var session = context.Tokenizer.CreateSession();
        var store = context.BlobStorage;
        var payload = new byte[8];
        var command = BuildRasterCommand(payload);

        session.Feed(command);
        session.Complete(CompletionReason.DataTimeout);

        var document = session.Document;

        DocumentAssertions.Equal(
            document,
            Protocol.EscPos,
            expectedElements:
            [
                new RasterImageContent(1, 8, 8, new MediaContent(new MediaMeta("image/png", 12, null), null))
            ]);
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
