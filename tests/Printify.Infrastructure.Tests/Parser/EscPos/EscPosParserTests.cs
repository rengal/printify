using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.EscPos;
using Printify.Tests.Shared.Document;
using System.Text;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests(EscPosParserFixture fixture) : IClassFixture<EscPosParserFixture>
{
    static EscPosParserTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private readonly IEscPosCommandTrieProvider trieProvider = fixture.TrieProvider;

    private void AssertScenario(EscPosScenario scenario, EscPosChunkStrategy strategy)
    {
        var elements = new List<Element>();
        var parser = new EscPosParser(trieProvider, elements.Add);
        foreach (var step in EscPosScenarioChunker.EnumerateChunks(scenario.Input, strategy))
        {
            parser.Feed(step.Buffer.Span, CancellationToken.None);
        }

        parser.Complete();
        DocumentAssertions.Equal(scenario.ExpectedElements, elements);
    }

    private void AssertScenarioAcrossAllStrategies(EscPosScenario scenario)
    {
        foreach (var strategy in EscPosChunkStrategies.All)
            AssertScenario(scenario, strategy);
    }
}
