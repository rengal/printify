using Printify.Tests.Shared.Epl;

namespace Printify.Infrastructure.Tests.Parser.Epl;

public partial class EplParserTests
{
    [Theory]
    [MemberData(nameof(EplGoldenCases.Cases), MemberType = typeof(EplGoldenCases))]
    public void Parser_Golden_Cases_ProduceExpectedElements(string caseId, byte[] payload)
    {
        Assert.True(EplGoldenCases.Expectations.TryGetValue(caseId, out var value));
        var scenario = new EplScenario(
            id: 260001,
            payload,
            value.expectedRequestElement,
            value.expectedPersistedElements,
            [value.expectedViewElements]);
        AssertScenario(scenario);
    }
}
