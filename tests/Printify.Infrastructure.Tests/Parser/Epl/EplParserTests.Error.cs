using Printify.Tests.Shared.Epl;

namespace Printify.Infrastructure.Tests.Parser.Epl;

public partial class EplParserTests
{
    [Theory]
    [MemberData(nameof(EplScenarioData.ErrorScenarios), MemberType = typeof(EplScenarioData))]
    public void Parser_Error_Scenarios_ProduceExpectedElements(EplScenario scenario)
    {
        AssertScenario(scenario);
    }
}
