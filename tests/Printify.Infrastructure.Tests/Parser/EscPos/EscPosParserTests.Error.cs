namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.ErrorScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Error_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenarioAcrossAllStrategies(scenario);
    }

    [Fact]
    public void Debug_Specific_Scenario2()
    {
        var scenario = EscPosScenarioData.ErrorScenarios.Cast<EscPosScenario>().ElementAt(3); // todo debugnow
        AssertScenarioAcrossAllStrategies(scenario);
    }
}
