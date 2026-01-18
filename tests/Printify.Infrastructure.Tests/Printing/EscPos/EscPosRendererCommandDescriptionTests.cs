using Printify.Application.Features.Printers.Documents.Canvas;
using Printify.Domain.Documents;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.Infrastructure.Printing.EscPos.Renderers;
using Printify.Tests.Shared.Document;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class EscPosRendererCommandDescriptionTests
{
    [Fact]
    public void EscPosRenderer_ProducesNonEmptyCommandDescriptions_ForKnownCommands()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Initialize(),
            new SetBoldMode(true),
            new SetUnderlineMode(false),
            new SetJustification(TextJustification.Center),
            new SetCodePage("437"),
            new SelectFont(FontNumber: 0, IsDoubleWidth: false, IsDoubleHeight: false),
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
            commands,
            null);

        // Act
        var renderer = new EscPosRenderer();
        var canvas = renderer.Render(document);
        var canvasDocument = RenderedDocument.From(document, canvas);

        // Assert - DebugInfo elements should have non-empty CommandDescription
        var debugElements = canvas.Items.OfType<DebugInfo>().ToList();
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
            new Initialize(),
            new SetBoldMode(true),
            new SetBoldMode(false),
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
            commands,
            null);

        // Act
        var renderer = new EscPosRenderer();
        var canvas = renderer.Render(document);

        // Assert - verify debug elements have non-empty descriptions
        var debugElements = canvas.Items.OfType<DebugInfo>().ToList();

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
