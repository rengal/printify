using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.BellScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Bell_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        var provider = new EscPosCommandTrieProvider();

        foreach (var strategy in EscPosChunkStrategies.All)
        {
            var elements = ParseScenario(provider, scenario, strategy);
            Assert.Equal(scenario.ExpectedElements, elements);
        }
    }
}
