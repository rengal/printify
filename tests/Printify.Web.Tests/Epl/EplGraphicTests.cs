using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Tests.Shared.Epl;

namespace Printify.Web.Tests.Epl;

/// <summary>
/// Tests for EPL graphics (GW command) in integration scenarios.
/// Note: Uses CombinedScenarios because GraphicScenarios don't include print commands
/// and won't complete in EPL page mode. The combined scenarios include both graphics
/// and print commands to properly test the full flow.
/// </summary>
public class EplGraphicTests(WebApplicationFactory<Program> factory) : EplTests(factory)
{
    [Theory]
    [MemberData(nameof(EplScenarioData.GraphicScenarios), MemberType = typeof(EplScenarioData))]
    public async Task Epl_Graphic_Scenarios_ProduceExpectedDocuments(EplScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
