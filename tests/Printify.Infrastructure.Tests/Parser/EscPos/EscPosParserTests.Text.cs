using System.Linq;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.TextScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Text_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenarioAcrossAllStrategies(scenario);
    }
}
