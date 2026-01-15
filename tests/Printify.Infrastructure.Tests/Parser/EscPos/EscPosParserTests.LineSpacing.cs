namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.LineSpacingScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_LineSpacing_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenario(scenario);
    }
}
