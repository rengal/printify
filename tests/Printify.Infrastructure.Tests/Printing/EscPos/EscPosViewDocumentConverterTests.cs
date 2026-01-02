using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.EscPos;
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
        var elements = scenario.ExpectedPersistedElements ?? scenario.ExpectedRequestElements;
        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Document.CurrentVersion,
            DateTimeOffset.UtcNow,
            Protocol.EscPos,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots,
            null,
            elements);

        var converter = new EscPosViewDocumentConverter();
        var viewDocument = converter.ToViewDocument(document);

        DocumentAssertions.EqualView(
            scenario.ExpectedViewElements,
            Protocol.EscPos,
            viewDocument,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
    }
}
