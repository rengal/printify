namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.FontStyleScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_FontStyle_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenarioAcrossAllStrategies(scenario);
    }
}
