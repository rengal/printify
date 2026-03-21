using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Tests.Shared.Epl;

namespace Printify.Web.Tests.Epl;

public class EplGoldenTests(WebApplicationFactory<Program> factory) : EplTests(factory)
{
    [Theory]
    [MemberData(nameof(EplGoldenCases.Cases), MemberType = typeof(EplGoldenCases))]
    public async Task Epl_Golden_Cases_ProduceExpectedDocuments(string caseId, byte[] payload)
    {
        Assert.True(EplGoldenCases.Expectations.TryGetValue(caseId, out var value));
        var scenario = new EplScenario(
            id: 260001,
            payload,
            value.expectedRequestElement,
            value.expectedPersistedElements,
            [value.expectedViewElements]);
        await RunScenarioAsync(scenario);
    }
}
