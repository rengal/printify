using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public class EscPosTextTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    public static TheoryData<EscPosScenario> TextScenarios =>
    [
        new(Input: "ABC\n"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine("ABC")
            ]),

        new(Input: "ABC"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine("ABC")
            ]),

        new(Input: "ABC"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine("ABC")
            ]),

        new(Input: "ABC\nDEF\nG"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine("ABC"),
                new TextLine("DEF"),
                new TextLine("G")
            ]),

        new(Input: "ABC"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine("ABC")
            ]),

        new(
            Input: Encoding.ASCII.GetBytes(new string('A', 10_000)),
            ExpectedElements:
            [
                new TextLine(new string('A', 10_000))
            ])
    ];

    [Theory]
    [MemberData(nameof(TextScenarios))]
    public async Task EscPos_Text_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
