using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Tests.Shared.Epl;

namespace Printify.Web.Tests.Epl;

public class EplTextTests(WebApplicationFactory<Program> factory) : EplTests(factory)
{
    [Theory]
    [MemberData(nameof(EplScenarioData.TextScenarios), MemberType = typeof(EplScenarioData))]
    public async Task Epl_Text_Scenarios_ProduceExpectedDocuments(EplScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
