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

    private readonly EscPosCommandTrieProvider trieProvider = fixture.TrieProvider;

    private void AssertScenario(EscPosScenario scenario)
    {
        var elements = new List<Element>();
        var parser = new EscPosParser(trieProvider, elements.Add);

        // Send byte by byte
        foreach (var b in scenario.Input)
        {
            parser.Feed(new[] { b }, CancellationToken.None);
        }

        parser.Complete();
        DocumentAssertions.Equal(scenario.ExpectedRequestElements, elements);
    }
}
