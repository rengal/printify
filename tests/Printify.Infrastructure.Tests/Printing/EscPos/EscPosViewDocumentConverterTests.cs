using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.EscPos.Renderers;
using Printify.Tests.Shared.Document;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class EscPosViewDocumentConverterTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.AllScenarios), MemberType = typeof(EscPosScenarioData))]
    public void EscPos_ViewConverter_Scenarios_ProduceExpectedView(EscPosScenario scenario)
    {
        var elements = scenario.ExpectedPersistedCommands ?? scenario.ExpectedRequestCommands;
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
            elements,
            null);

        var renderer = new EscPosRenderer();
        var canvases = renderer.Render(document);
        var canvasDocument = RenderedDocument.From(document, canvases);

        DocumentAssertions.EqualView(
            scenario.ExpectedCanvasElements,
            Protocol.EscPos,
            canvasDocument,
            canvases[0].WidthInDots,
            canvases[0].HeightInDots);
    }
}
