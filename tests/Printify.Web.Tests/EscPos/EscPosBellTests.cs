using Microsoft.AspNetCore.Mvc.Testing;

namespace Printify.Web.Tests.EscPos;

public class EscPosBellTests(WebApplicationFactory<Program> factory): EscPosTests(factory)
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.BellScenarios), MemberType = typeof(EscPosScenarioData))]
    public async Task EscPos_Bell_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
