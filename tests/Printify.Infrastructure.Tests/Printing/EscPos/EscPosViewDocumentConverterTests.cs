using Printify.Application.Features.Printers.Documents.Canvas;
using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.EscPos.Renderers;
using Printify.Tests.Shared.Document;
using Printify.Tests.Shared.EscPos;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class EscPosViewDocumentConverterTests
{
    private const int DefaultPrinterWidthInDots = 512;
    private static readonly int? DefaultPrinterHeightInDots = null;

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
            elements,
            null);

        var renderer = new EscPosRenderer();
        var canvas = renderer.Render(document);
        var canvasDocument = RenderedDocument.From(document, canvas);

        try
        {
            DocumentAssertions.EqualView(
                scenario.ExpectedCanvasElements,
                Protocol.EscPos,
                canvasDocument,
                canvas.WidthInDots,
                canvas.HeightInDots);
        }
        catch (Exception e)
        {
            Console.WriteLine(e); //todo debugnow
            throw;
        }
    }
}
