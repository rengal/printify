using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.Epl;
using Printify.Tests.Shared.Document;
using Printify.Tests.Shared.Epl;
using System.Text;

namespace Printify.Infrastructure.Tests.Parser.Epl;

public partial class EplParserTests(EplParserFixture fixture) : IClassFixture<EplParserFixture>
{
    static EplParserTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private readonly EplCommandTrieProvider trieProvider = fixture.TrieProvider;

    private void AssertScenario(EplScenario scenario, EplChunkStrategy strategy)
    {
        var elements = new List<Element>();
        var parser = new EplParser(elements.Add);
        foreach (var step in EplScenarioChunker.EnumerateChunks(scenario.Input, strategy))
        {
            parser.Feed(step.Buffer.Span, CancellationToken.None);
        }

        parser.Complete();
        DocumentAssertions.Equal(scenario.ExpectedRequestElements, elements);
    }

    private void AssertScenarioAcrossAllStrategies(EplScenario scenario)
    {
        AssertScenario(scenario, EplChunkStrategies.SingleByte);
    }
}
