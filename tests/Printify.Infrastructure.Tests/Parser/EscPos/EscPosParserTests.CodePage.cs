namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.CodePageScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_CodePage_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenarioAcrossAllStrategies(scenario);
    }
}
