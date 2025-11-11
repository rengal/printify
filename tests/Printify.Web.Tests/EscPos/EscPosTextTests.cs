using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public class EscPosTextTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    [Theory]
    [MemberData(nameof(EscPosScenarioData.TextScenarios), MemberType = typeof(EscPosScenarioData))]
    public async Task EscPos_Text_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
