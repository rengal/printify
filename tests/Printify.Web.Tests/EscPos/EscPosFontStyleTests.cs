using Microsoft.AspNetCore.Mvc.Testing;

namespace Printify.Web.Tests.EscPos;

public class EscPosFontStyleTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.FontStyleScenarios), MemberType = typeof(EscPosScenarioData))]
    public async Task EscPos_FontStyle_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
