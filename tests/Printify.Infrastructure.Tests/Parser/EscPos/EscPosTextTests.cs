using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public class EscPosTextTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.TextScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Text_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        var provider = new EscPosCommandTrieProvider();

        foreach (var strategy in EscPosChunkStrategies.All)
        {
            try
            {
                var elements = ParseScenario(provider, scenario, strategy);
                Assert.Equal(scenario.ExpectedElements, elements);
            }
            catch
            {
                var elements = ParseScenario(provider, scenario, strategy);
                Assert.Equal(scenario.ExpectedElements, elements);
            }
        }
    }

    private static IReadOnlyList<Element> ParseScenario(
        IEscPosCommandTrieProvider provider,
        EscPosScenario scenario,
        EscPosChunkStrategy strategy)
    {
        var elements = new List<Element>();
        var parser = new EscPosParser(provider, elements.Add);

        foreach (var step in EscPosScenarioChunker.EnumerateChunks(scenario.Input, strategy))
        {
            parser.Feed(step.Buffer.Span, CancellationToken.None);
        }

        parser.Complete();
        return elements;
    }
}
