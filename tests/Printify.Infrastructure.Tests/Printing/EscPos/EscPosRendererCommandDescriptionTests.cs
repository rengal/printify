using Printify.Domain.Documents;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.Infrastructure.Printing.EscPos.Renderers;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class EscPosRendererCommandDescriptionTests
{
    [Fact]
    public void EscPosRenderer_ProducesNonEmptyCommandDescriptions_ForKnownCommands()
    {
        // Arrange
        var commands = new List<Command>
        {
            new EscPosInitialize(),
            new EscPosSetBoldMode(true),
            new EscPosSetUnderlineMode(false),
            new EscPosSetJustification(EscPosTextJustification.Center),
            new EscPosSetCodePage("437"),
            new EscPosSelectFont(FontNumber: 0, IsDoubleWidth: false, IsDoubleHeight: false),
        };

        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.EscPos,
            null,
            0,
            0,
            EscPosSpecs.DefaultCanvasWidth,
            null,
            commands,
            null);

        // Act
        var renderer = new EscPosRenderer();
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
    public void EscPosRenderer_ProducesExpectedDescriptions_ForSpecificCommands()
    {
        // Arrange
        var commands = new List<Command>
        {
            new EscPosInitialize(),
            new EscPosSetBoldMode(true),
            new EscPosSetBoldMode(false),
        };

        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.EscPos,
            null,
            0,
            0,
            EscPosSpecs.DefaultCanvasWidth,
            null,
            commands,
            null);

        // Act
        var renderer = new EscPosRenderer();
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
