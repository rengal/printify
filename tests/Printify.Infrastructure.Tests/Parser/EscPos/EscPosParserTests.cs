using System.Text;
using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    static EscPosParserTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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

    private static IReadOnlyList<Element> ParseScenarioAcrossStrategies(
        IEscPosCommandTrieProvider provider,
        EscPosScenario scenario)
    {
        IReadOnlyList<Element>? baseline = null;
        foreach (var strategy in EscPosChunkStrategies.All)
        {
            var result = ParseScenario(provider, scenario, strategy);
            if (baseline is null)
            {
                baseline = result;
                continue;
            }

            Assert.Equal(baseline, result);
        }

        return baseline ?? Array.Empty<Element>();
    }
}
