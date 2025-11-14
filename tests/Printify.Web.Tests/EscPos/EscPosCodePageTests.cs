using Microsoft.AspNetCore.Mvc.Testing;

namespace Printify.Web.Tests.EscPos;

public class EscPosCodePageTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.CodePageScenarios), MemberType = typeof(EscPosScenarioData))]
    public async Task EscPos_CodePage_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
