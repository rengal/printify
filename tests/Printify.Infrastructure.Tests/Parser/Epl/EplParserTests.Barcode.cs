using Printify.Tests.Shared.Epl;

namespace Printify.Infrastructure.Tests.Parser.Epl;

public partial class EplParserTests
{
    [Theory]
    [MemberData(nameof(EplScenarioData.BarcodeScenarios), MemberType = typeof(EplScenarioData))]
    public void Parser_Barcode_Scenarios_ProduceExpectedElements(EplScenario scenario)
    {
        AssertScenario(scenario);
    }
}
