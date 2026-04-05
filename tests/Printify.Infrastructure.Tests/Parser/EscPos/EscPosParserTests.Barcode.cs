namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.BarcodeScenarios), MemberType = typeof(EscPosScenarioData))]
    public void Parser_Barcode_Scenarios_ProduceExpectedElements(EscPosScenario scenario)
    {
        AssertScenario(scenario);
    }
}
