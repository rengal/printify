using Microsoft.AspNetCore.Mvc.Testing;

namespace Printify.Web.Tests.EscPos;

public class EscPosGoldenTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    [Theory]
    [MemberData(nameof(EscPosGoldenCases.Cases), MemberType = typeof(EscPosGoldenCases))]
    public async Task EscPos_Golden_Cases_ProduceExpectedDocuments(string caseId, byte[] payload)
    {
        Assert.True(EscPosGoldenCases.Expectations.TryGetValue(caseId, out var value));
        var scenario = new EscPosScenario(
            id: 250001,
            payload,
            value.expectedRequestElement,
            value.expectedPersistedElements,
            value.expectedViewElements);
        await RunScenarioAsync(scenario);
    }
}
