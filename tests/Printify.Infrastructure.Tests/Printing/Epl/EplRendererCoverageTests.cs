using Printify.Domain.Documents;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Media;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Infrastructure.Printing.Epl.Renderers;
using Printify.Tests.Shared;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Infrastructure.Tests.Printing.Epl;

public sealed class EplRendererCoverageTests
{
    private static DomainMedia CreateTestMedia() =>
        DomainMedia.CreateDefaultPng(100);

    [Fact]
    public void Renderer_DoesNotThrowException_ForAllEplCommandTypes()
    {
        // Create sample commands to test
        var commands = new List<Command>
        {
            // Config commands
            new EplClearBuffer(),
            new EplCarriageReturn(),
            new EplLineFeed(),
            new EplSetLabelWidth(500),
            new EplSetLabelHeight(300, 26),
            new EplSetPrintSpeed(3),
            new EplSetPrintDarkness(10),
            new SetPrintDirection(EplPrintDirection.TopToBottom),
            new SetPrintDirection(EplPrintDirection.BottomToTop),
            new EplSetInternationalCharacter(0),
            new EplSetInternationalCharacter(8),

            // Text commands
            new EplScalableText(10, 20, 0, 1, 1, 1, 'N', "test"u8.ToArray()),
            new EplScalableText(10, 20, 1, 2, 2, 2, 'R', "test"u8.ToArray()),
            new EplDrawHorizontalLine(10, 20, 2, 100),

            // Barcode commands
            new PrintBarcode(10, 20, 0, "CODE128", 2, 100, 'N', "12345"),
            new PrintBarcode(10, 20, 1, "CODE39", 3, 80, 'B', "ABC"),
            new EplPrintBarcode(10, 20, 0, "CODE128", 2, 100, 'N', "12345", CreateTestMedia()),
            // Note: EplPrintBarcodeUpload is excluded because it's an upload command not meant to be rendered

            // Graphics commands
            new EplRasterImage(10, 20, 100, 50, CreateTestMedia()),
            // Note: EplRasterImageUpload is excluded because it's an upload command not meant to be rendered

            // Shape commands
            new EplDrawBox(10, 20, 2, 100, 80),

            // Error commands (placed before Print to be included in canvases)
            new EplParseError("ERR_CODE", "Test error message"),
            new EplPrinterError("Test printer error"),

            // Print command to finalize the canvas
            new EplPrint(1),

            // Additional commands after print to test multiple canvases
            new EplScalableText(10, 20, 0, 1, 1, 1, 'N', "test2"u8.ToArray()),
            new EplDrawBox(20, 40, 2, 150, 100),
            new EplPrint(1),
        };

        // Verify the list is complete via reflection (excluding upload commands)
        CommandTestExtensions.VerifyAllRenderableEplCommandTypesAreTested(commands);

        // Create document and render
        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.Epl,
            null,
            0,
            0,
            EplSpecs.DefaultCanvasWidth,
            null,
            commands,
            null);

        // Act & Assert - should not throw
        var renderer = new EplRenderer();
        var exception = Record.Exception(() => renderer.Render(document));

        Assert.Null(exception);
    }

    [Fact]
    public void Renderer_ProducesValidCanvases_ForAllEplCommandTypes()
    {
        // Create sample commands to test
        var commands = new List<Command>
        {
            // Config commands
            new EplClearBuffer(),
            new EplCarriageReturn(),
            new EplLineFeed(),
            new EplSetLabelWidth(500),
            new EplSetLabelHeight(300, 26),
            new EplSetPrintSpeed(3),
            new EplSetPrintDarkness(10),
            new SetPrintDirection(EplPrintDirection.TopToBottom),
            new SetPrintDirection(EplPrintDirection.BottomToTop),
            new EplSetInternationalCharacter(0),
            new EplSetInternationalCharacter(8),

            // Text commands
            new EplScalableText(10, 20, 0, 1, 1, 1, 'N', "test"u8.ToArray()),
            new EplScalableText(10, 20, 1, 2, 2, 2, 'R', "test"u8.ToArray()),
            new EplDrawHorizontalLine(10, 20, 2, 100),

            // Barcode commands
            new PrintBarcode(10, 20, 0, "CODE128", 2, 100, 'N', "12345"),
            new PrintBarcode(10, 20, 1, "CODE39", 3, 80, 'B', "ABC"),
            new EplPrintBarcode(10, 20, 0, "CODE128", 2, 100, 'N', "12345", CreateTestMedia()),

            // Graphics commands
            new EplRasterImage(10, 20, 100, 50, CreateTestMedia()),

            // Shape commands
            new EplDrawBox(10, 20, 2, 100, 80),

            // Error commands (placed before Print to be included in canvases)
            new EplParseError("ERR_CODE", "Test error message"),
            new EplPrinterError("Test printer error"),

            // Print command to finalize the canvas
            new EplPrint(1),

            // Additional commands after print to test multiple canvases
            new EplScalableText(10, 20, 0, 1, 1, 1, 'N', "test2"u8.ToArray()),
            new EplDrawBox(20, 40, 2, 150, 100),
            new EplPrint(1),
        };

        // Verify the list is complete via reflection (excluding upload commands)
        CommandTestExtensions.VerifyAllRenderableEplCommandTypesAreTested(commands);

        // Create document and render
        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.Epl,
            null,
            0,
            0,
            EplSpecs.DefaultCanvasWidth,
            null,
            commands,
            null);

        // Act
        var renderer = new EplRenderer();
        var canvases = renderer.Render(document);

        // Assert
        Assert.NotNull(canvases);
        Assert.NotEmpty(canvases);

        // Each canvas should have at least one element
        foreach (var canvas in canvases)
        {
            Assert.NotNull(canvas.Items);
            Assert.NotEmpty(canvas.Items);
        }
    }
}
