using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
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
}
