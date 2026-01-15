using Printify.Tests.Shared.Epl;

namespace Printify.Infrastructure.Tests.Parser.Epl;

public partial class EplParserTests
{
    [Theory]
    [MemberData(nameof(EplScenarioData.PrintScenarios), MemberType = typeof(EplScenarioData))]
    public void Parser_Print_Scenarios_ProduceExpectedElements(EplScenario scenario)
    {
        AssertScenario(scenario);
    }
}
