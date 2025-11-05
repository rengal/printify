using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public class EscPosBellTests(WebApplicationFactory<Program> factory): EscPosTests(factory)
{
    public static TheoryData<EscPosScenario> BellScenarios =>
    [
        new(Input: [0x07],
            ExpectedElements:
            [
                new Bell(1)
            ]),
        new(Input: Enumerable.Repeat((byte)0x07, 10).ToArray(),
            ExpectedElements: Enumerable.Range(1, 10).Select(i => new Bell(i)).ToArray())
    ];

    [Theory]
    [MemberData(nameof(BellScenarios))]
    public async Task EscPos_Bell_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}