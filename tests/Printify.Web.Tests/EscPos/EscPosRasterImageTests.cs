using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Printify.Web.Tests.EscPos;

public class EscPosRasterImageTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.RasterImageScenarios), MemberType = typeof(EscPosScenarioData))]
    public async Task EscPos_RasterImage_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        Debug.Print("EscPos_RasterImage_Scenarios_ProduceExpectedDocuments"); //todo debugnow
        await RunScenarioAsync(scenario);
    }
}
