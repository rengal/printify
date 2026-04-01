namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.PrintAndFeedLinesScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_PrintAndFeedLines_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenario(scenario);
    }
}
