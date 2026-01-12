using Printify.Tests.Shared.Epl;

namespace Printify.Infrastructure.Tests.Parser.Epl;

public partial class EplParserTests
{
    [Theory]
    [MemberData(nameof(EplScenarioData.TextScenarios), MemberType = typeof(EplScenarioData))]
    public void Parser_Text_Scenarios_ProduceExpectedElements(EplScenario scenario)
    {
        AssertScenarioAcrossAllStrategies(scenario);
    }
}
