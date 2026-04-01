using Microsoft.AspNetCore.Mvc.Testing;

namespace Printify.Web.Tests.EscPos;

public class EscPosSetFontTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.SetFontScenarios), MemberType = typeof(EscPosScenarioData))]
    public async Task EscPos_SetFont_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }

    [Theory]
    [MemberData(nameof(EscPosScenarioData.PrintAndFeedLinesScenarios), MemberType = typeof(EscPosScenarioData))]
    public async Task EscPos_PrintAndFeedLines_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
