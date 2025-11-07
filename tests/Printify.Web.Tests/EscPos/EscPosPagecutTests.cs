using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public class EscPosPagecutTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    public static TheoryData<EscPosScenario> PagecutScenarios =>
    [
        new(
            Input: [Esc, (byte)'i'],
            ExpectedElements:
            [
                new Pagecut()
            ]),
        new(
            Input: [Gs, 0x56, 0x00],
            ExpectedElements:
            [
                new Pagecut()
            ])
    ];

    [Theory]
    [MemberData(nameof(PagecutScenarios))]
    public async Task EscPos_Pagecut_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}

