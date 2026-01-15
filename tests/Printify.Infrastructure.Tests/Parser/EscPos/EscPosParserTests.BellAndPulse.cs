namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.BellScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Bell_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenario(scenario);
    }

    [Theory]
    [MemberData(nameof(EscPosScenarioData.PulseScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Pulse_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenario(scenario);
    }
}
