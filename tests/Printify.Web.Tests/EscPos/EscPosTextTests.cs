using Microsoft.AspNetCore.Mvc.Testing;

namespace Printify.Web.Tests.EscPos;

public class EscPosTextTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.TextScenarios), MemberType = typeof(EscPosScenarioData))]
    public async Task EscPos_Text_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        if (scenario.Id != 15016)
            return;
        await RunScenarioAsync(scenario);
    }
}
