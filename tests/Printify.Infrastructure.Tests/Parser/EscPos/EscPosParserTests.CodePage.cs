using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.CodePageScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_CodePage_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        var provider = new EscPosCommandTrieProvider();

        var elements = ParseScenarioAcrossStrategies(provider, scenario);
        Assert.Equal(scenario.ExpectedElements, elements);
    }
}
