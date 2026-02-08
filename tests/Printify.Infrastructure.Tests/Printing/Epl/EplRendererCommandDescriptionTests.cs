using Printify.Application.Features.Printers.Documents.Canvas;
using Printify.Domain.Documents;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Infrastructure.Printing.Epl.Renderers;
using Printify.Tests.Shared.Document;

namespace Printify.Infrastructure.Tests.Printing.Epl;

public sealed class EplRendererCommandDescriptionTests
{
    [Fact]
    public void EplRenderer_ProducesNonEmptyCommandDescriptions_ForKnownCommands()
    {
        // Arrange
        var commands = new List<Command>
        {
            new EplClearBuffer(),
            new EplSetLabelWidth(500),
            new EplSetLabelHeight(300, 0),
            new EplSetPrintSpeed(3),
            new EplSetPrintDarkness(10),
            new SetPrintDirection(EplPrintDirection.TopToBottom),
        };

        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.Epl,
            null,
            0,
            0,
            500,  // WidthInDots
            300,  // HeightInDots
            commands,
            null);

        // Act
        var renderer = new EplRenderer();
        var canvas = renderer.Render(document);
        var canvasDocument = RenderedDocument.From(document, canvas);

        // Assert - DebugInfo elements should have non-empty CommandDescription
        var debugElements = canvas[0].Items.OfType<DebugInfo>().ToList();
        Assert.NotEmpty(debugElements);
        Assert.All(debugElements, element =>
        {
            Assert.NotNull(element.CommandDescription);
            Assert.NotEmpty(element.CommandDescription);
            Assert.All(element.CommandDescription, line =>
            {
                Assert.False(string.IsNullOrWhiteSpace(line),
                    $"CommandDescription for {element.DebugType} contains empty/whitespace line");
            });
        });
    }

    [Fact]
    public void EplRenderer_ProducesExpectedDescriptions_ForSpecificCommands()
    {
        // Arrange
        var commands = new List<Command>
        {
            new EplClearBuffer(),
            new EplSetLabelWidth(500),
            new EplPrint(1),
        };

        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.Epl,
            null,
            0,
            0,
            500,  // WidthInDots
            300,  // HeightInDots
            commands,
            null);

        // Act
        var renderer = new EplRenderer();
        var canvas = renderer.Render(document);

        // Assert - verify debug elements have non-empty descriptions
        var debugElements = canvas[0].Items.OfType<DebugInfo>().ToList();

        foreach (var element in debugElements)
        {
            Assert.NotNull(element.CommandDescription);
            Assert.NotEmpty(element.CommandDescription);
            foreach (var line in element.CommandDescription)
            {
                Assert.False(string.IsNullOrWhiteSpace(line));
            }
        }
    }

    [Fact]
    public void EplRenderer_ProducesDescriptions_ForTextAndBarcodeCommands()
    {
        // Arrange
        var textBytes = System.Text.Encoding.GetEncoding(437).GetBytes("Test");
        var commands = new List<Command>
        {
            new EplScalableText(10, 20, 0, 2, 1, 1, 'N', textBytes),
            new PrintBarcode(50, 60, 0, "128", 2, 100, 'N', "ABC123"),
        };

        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.Epl,
            null,
            0,
            0,
            500,  // WidthInDots
            300,  // HeightInDots
            commands,
            null);

        // Act
        var renderer = new EplRenderer();
        var canvas = renderer.Render(document);

        // Assert - verify debug elements have non-empty descriptions
        var debugElements = canvas[0].Items.OfType<DebugInfo>().ToList();

        foreach (var element in debugElements)
        {
            Assert.NotNull(element.CommandDescription);
            Assert.NotEmpty(element.CommandDescription);
            foreach (var line in element.CommandDescription)
            {
                Assert.False(string.IsNullOrWhiteSpace(line));
            }
        }
    }
}
