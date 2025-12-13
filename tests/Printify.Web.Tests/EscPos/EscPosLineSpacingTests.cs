using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public class EscPosLineSpacingTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    public static TheoryData<EscPosScenario> LineSpacingScenarios =>
    [
        new(
            Input: [Esc, 0x33, 0x40],
            ExpectedRequestElements:
            [
                new SetLineSpacing(0x40)
            ]),
        new(
            Input: [Esc, 0x32],
            ExpectedRequestElements:
            [
                new ResetLineSpacing()
            ])
    ];

    [Theory]
    [MemberData(nameof(LineSpacingScenarios))]
    public async Task EscPos_LineSpacing_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
