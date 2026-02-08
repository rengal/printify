using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.Epl.Renderers;
using Printify.Tests.Shared;

namespace Printify.Infrastructure.Tests.Printing.Epl;

public sealed class EplRendererCoverageTests
{
    [Fact]
    public void Renderer_DoesNotThrowException_ForAllEplCommandTypes()
    {
        // Create sample commands to test
        var commands = EplTestCommandFactory.CreateSampleEplCommands(withUploadCommands: false);

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
        var commands = EplTestCommandFactory.CreateSampleEplCommands(withUploadCommands: false);

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
