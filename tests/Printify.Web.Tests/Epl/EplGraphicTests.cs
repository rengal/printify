using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Application.Printing.Events;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Tests.Shared.Epl;
using Printify.TestServices;
using SkiaSharp;

namespace Printify.Web.Tests.Epl;

/// <summary>
/// Tests for EPL graphics (GW command) in integration scenarios.
/// Note: Uses CombinedScenarios because GraphicScenarios don't include print commands
/// and won't complete in EPL page mode. The combined scenarios include both graphics
/// and print commands to properly test the full flow.
/// </summary>
public class EplGraphicTests(WebApplicationFactory<Program> factory) : EplTests(factory)
{
    [Theory]
    [MemberData(nameof(EplScenarioData.GraphicScenarios), MemberType = typeof(EplScenarioData))]
    public async Task Epl_Graphic_Scenarios_ProduceExpectedDocuments(EplScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }

    [Fact]
    public async Task Epl_Graphic_PersistedMedia_HasSetBitsBlackAndUnsetBitsTransparent()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(Factory);
        await AuthenticateAsync(environment, "epl-raster-persistence-check");

        var printerId = Guid.NewGuid();
        var channel = await CreatePrinterAsync(
            environment,
            printerId,
            "Epl Raster Persistence Check",
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);

        var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();

        // GW payload for 8x2 image + P1 to flush the page.
        var payload = new byte[]
        {
            (byte)'G', (byte)'W', (byte)'1', (byte)'0', (byte)',', (byte)'5', (byte)'0', (byte)',',
            (byte)'1', (byte)',', (byte)'2', (byte)',',
            0b11100000,
            0b00011000,
            (byte)'\n',
            (byte)'P', (byte)'1', (byte)'\n'
        };

        await channel.SendToServerAsync(payload, CancellationToken.None);
        await channel.CloseAsync(ChannelClosedReason.Completed);

        var hasEvent = await streamEnumerator.MoveNextAsync();
        Assert.True(hasEvent);

        var document = streamEnumerator.Current.Document;
        var rasterCommand = Assert.Single(document.Commands.OfType<EplRasterImage>());

        await using var scope = environment.CreateScope();
        var mediaStorage = scope.ServiceProvider.GetRequiredService<IMediaStorage>();
        await using var mediaStream = await mediaStorage.OpenReadAsync(rasterCommand.Media.Id, CancellationToken.None);
        Assert.NotNull(mediaStream);

        using var bytes = new MemoryStream();
        await mediaStream.CopyToAsync(bytes);
        using var bitmap = SKBitmap.Decode(bytes.ToArray());
        Assert.NotNull(bitmap);
        Assert.Equal(8, bitmap!.Width);
        Assert.Equal(2, bitmap.Height);

        AssertTransparent(bitmap.GetPixel(0, 0));
        AssertTransparent(bitmap.GetPixel(1, 0));
        AssertTransparent(bitmap.GetPixel(2, 0));
        AssertBlackOpaque(bitmap.GetPixel(3, 0));
        AssertBlackOpaque(bitmap.GetPixel(4, 0));
        AssertBlackOpaque(bitmap.GetPixel(5, 0));
        AssertBlackOpaque(bitmap.GetPixel(6, 0));
        AssertBlackOpaque(bitmap.GetPixel(7, 0));

        AssertBlackOpaque(bitmap.GetPixel(0, 1));
        AssertBlackOpaque(bitmap.GetPixel(1, 1));
        AssertBlackOpaque(bitmap.GetPixel(2, 1));
        AssertTransparent(bitmap.GetPixel(3, 1));
        AssertTransparent(bitmap.GetPixel(4, 1));
        AssertBlackOpaque(bitmap.GetPixel(5, 1));
        AssertBlackOpaque(bitmap.GetPixel(6, 1));
        AssertBlackOpaque(bitmap.GetPixel(7, 1));
    }

    private static void AssertBlackOpaque(SKColor pixel)
    {
        Assert.True(pixel.Red == 0 && pixel.Green == 0 && pixel.Blue == 0 && pixel.Alpha == 255);
    }

    private static void AssertTransparent(SKColor pixel)
    {
        Assert.True(pixel.Alpha == 0);
    }
}
