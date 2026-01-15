namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.PagecutScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Pagecut_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenario(scenario);
    }
}
