using Printify.Domain.Documents;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Infrastructure.Printing.Epl.Renderers;
using Printify.Tests.Shared;

namespace Printify.Infrastructure.Tests.Printing.Epl;

public sealed class EplRendererCoverageTests
{
    [Fact]
    public void Renderer_DoesNotThrowException_ForAllEplCommandTypes()
    {
        // Create sample commands to test
        var commands = new List<Command>
        {
            // Config commands
            new ClearBuffer(),
            new CarriageReturn(),
            new LineFeed(),
            new SetLabelWidth(500),
            new SetLabelHeight(300, 26),
            new SetPrintSpeed(3),
            new SetPrintDarkness(10),
            new SetPrintDirection(PrintDirection.TopToBottom),
            new SetPrintDirection(PrintDirection.BottomToTop),
            new SetInternationalCharacter(0),
            new SetInternationalCharacter(8),

            // Text commands
            new ScalableText(10, 20, 0, 1, 1, 1, 'N', "test"u8.ToArray()),
            new ScalableText(10, 20, 1, 2, 2, 2, 'R', "test"u8.ToArray()),
            new DrawHorizontalLine(10, 20, 2, 100),

            // Barcode commands
            new PrintBarcode(10, 20, 0, "CODE128", 2, 100, 'N', "12345"),
            new PrintBarcode(10, 20, 1, "CODE39", 3, 80, 'B', "ABC"),

            // Graphics commands
            new PrintGraphic(10, 20, 100, 50, new byte[20]),

            // Shape commands
            new DrawLine(10, 20, 2, 100, 80),

            // Print commands
            new Print(1),
            new Print(2),

            // Error commands
            new EplParseError("ERR_CODE", "Test error message"),
            new EplPrinterError("Test printer error"),
        };

        // Verify the list is complete via reflection
        CommandTestExtensions.VerifyAllEplCommandTypesAreTested(commands);

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
            new ClearBuffer(),
            new CarriageReturn(),
            new LineFeed(),
            new SetLabelWidth(500),
            new SetLabelHeight(300, 26),
            new SetPrintSpeed(3),
            new SetPrintDarkness(10),
            new SetPrintDirection(PrintDirection.TopToBottom),
            new SetPrintDirection(PrintDirection.BottomToTop),
            new SetInternationalCharacter(0),
            new SetInternationalCharacter(8),

            // Text commands
            new ScalableText(10, 20, 0, 1, 1, 1, 'N', "test"u8.ToArray()),
            new ScalableText(10, 20, 1, 2, 2, 2, 'R', "test"u8.ToArray()),
            new DrawHorizontalLine(10, 20, 2, 100),

            // Barcode commands
            new PrintBarcode(10, 20, 0, "CODE128", 2, 100, 'N', "12345"),
            new PrintBarcode(10, 20, 1, "CODE39", 3, 80, 'B', "ABC"),

            // Graphics commands
            new PrintGraphic(10, 20, 100, 50, new byte[20]),

            // Shape commands
            new DrawLine(10, 20, 2, 100, 80),

            // Print commands
            new Print(1),
            new Print(2),

            // Error commands
            new EplParseError("ERR_CODE", "Test error message"),
            new EplPrinterError("Test printer error"),
        };

        // Verify the list is complete via reflection
        CommandTestExtensions.VerifyAllEplCommandTypesAreTested(commands);

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
