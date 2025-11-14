using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.BellScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Bell_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        var provider = new EscPosCommandTrieProvider();
        var elements = ParseScenarioAcrossStrategies(provider, scenario);
        Assert.Equal(scenario.ExpectedElements, elements);
    }

    [Theory]
    [MemberData(nameof(EscPosScenarioData.PulseScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Pulse_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        var provider = new EscPosCommandTrieProvider();
        var elements = ParseScenarioAcrossStrategies(provider, scenario);
        Assert.Equal(scenario.ExpectedElements, elements);
    }
}
