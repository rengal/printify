using Microsoft.AspNetCore.Mvc.Testing;

namespace Printify.Web.Tests.EscPos;

public class EscPosPagecutTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.PagecutScenarios), MemberType = typeof(EscPosScenarioData))]
    public async Task EscPos_Pagecut_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}

