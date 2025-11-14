using System.Text;
using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests : IClassFixture<EscPosParserFixture>
{
    static EscPosParserTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private readonly IEscPosCommandTrieProvider trieProvider;

    public EscPosParserTests(EscPosParserFixture fixture)
    {
        trieProvider = fixture.TrieProvider;
    }

    private void AssertScenarioAcrossAllStrategies(EscPosScenario scenario)
    {
        IReadOnlyList<Element>? baseline = null;

        foreach (var strategy in EscPosChunkStrategies.All)
        {
            var elements = new List<Element>();
            var parser = new EscPosParser(trieProvider, elements.Add);
            foreach (var step in EscPosScenarioChunker.EnumerateChunks(scenario.Input, strategy))
            {
                parser.Feed(step.Buffer.Span, CancellationToken.None);
            }

            parser.Complete();

            baseline ??= elements;
            Assert.Equal(baseline, elements);
        }
    }
}
