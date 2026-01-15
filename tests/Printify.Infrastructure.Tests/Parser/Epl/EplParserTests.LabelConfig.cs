using Printify.Tests.Shared.Epl;

namespace Printify.Infrastructure.Tests.Parser.Epl;

public partial class EplParserTests
{
    [Theory]
    [MemberData(nameof(EplScenarioData.LabelConfigScenarios), MemberType = typeof(EplScenarioData))]
    public void Parser_LabelConfig_Scenarios_ProduceExpectedElements(EplScenario scenario)
    {
        AssertScenario(scenario);
    }
}
