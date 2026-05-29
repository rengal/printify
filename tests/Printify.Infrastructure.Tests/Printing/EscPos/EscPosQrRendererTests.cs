using Printify.Domain.Documents;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.Infrastructure.Printing.EscPos.Renderers;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class EscPosQrRendererTests
{
    [Fact]
    public void Render_AddsQrQuietZoneAsVerticalLayoutSpacing()
    {
        const int moduleSizeInDots = 6;
        const int quietZoneInDots = moduleSizeInDots * 4;
        var qrMedia = DomainMedia.CreateDefaultPng(100);
        var imageMedia = DomainMedia.CreateDefaultPng(10);
        var document = CreateDocument(
        [
            new EscPosSetQrModuleSize(moduleSizeInDots),
            new EscPosPrintQrCode(Data: "QR", Width: 100, Height: 100, Media: qrMedia),
            new EscPosRasterImageGs7630(Width: 10, Height: 10, Media: imageMedia)
        ]);

        var renderer = new EscPosRenderer();
        var canvas = Assert.Single(renderer.Render(document));
        var images = canvas.Items.OfType<ImageElement>().ToArray();

        Assert.Equal(2, images.Length);
        Assert.Equal(quietZoneInDots, images[0].Y);
        Assert.Equal(100, images[0].Height);
        Assert.Equal(100 + quietZoneInDots * 2, images[1].Y);
    }

    [Theory]
    [InlineData(EscPosTextJustification.Left, 0)]
    [InlineData(EscPosTextJustification.Center, 206)]
    [InlineData(EscPosTextJustification.Right, 412)]
    public void Render_AlignsQrUsingImageElementX(
        EscPosTextJustification justification,
        int expectedX)
    {
        var qrMedia = DomainMedia.CreateDefaultPng(100);
        var document = CreateDocument(
        [
            new EscPosSetJustification(justification),
            new EscPosPrintQrCode(Data: "QR", Width: 100, Height: 100, Media: qrMedia)
        ]);

        var renderer = new EscPosRenderer();
        var canvas = Assert.Single(renderer.Render(document));
        var image = Assert.Single(canvas.Items.OfType<ImageElement>());

        Assert.Equal(expectedX, image.X);
    }


    private static Document CreateDocument(IReadOnlyList<Command> commands)
    {
        return new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.EscPos,
            null,
            0,
            0,
            512,
            null,
            commands,
            null);
    }
}
