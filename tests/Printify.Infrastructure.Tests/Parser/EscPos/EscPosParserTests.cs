using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
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