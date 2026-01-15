using Printify.Tests.Shared.Epl;

namespace Printify.Infrastructure.Tests.Parser.Epl;

public partial class EplParserTests
{
    [Theory]
    [MemberData(nameof(EplScenarioData.ShapeScenarios), MemberType = typeof(EplScenarioData))]
    public void Parser_Shape_Scenarios_ProduceExpectedElements(EplScenario scenario)
    {
        AssertScenario(scenario);
    }
}
