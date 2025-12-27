namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.ErrorScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Error_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenarioAcrossAllStrategies(scenario);
    }
}
