namespace Printify.Infrastructure.Tests.Parser.EscPos;

public partial class EscPosParserTests
{
    [Theory]
    [MemberData(nameof(EscPosGoldenCases.Cases), MemberType = typeof(EscPosGoldenCases))]
    public void Parser_Golden_Cases_ProduceExpectedElements(string caseId, byte[] payload)
    {
        Assert.True(EscPosGoldenCases.Expectations.TryGetValue(caseId, out var value));
        var scenario = new EscPosScenario(
            id: 250001,
            payload,
            value.expectedRequestElement,
            value.expectedPersistedElements,
            value.expectedViewElements);
        AssertScenarioAcrossAllStrategies(scenario);
    }
}
