using System.Text;
using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public sealed partial class EscPosTests
{
    public static TheoryData<EscPosScenario> TextScenarios =>
    [
        new(Input: "ABC\n"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine(1, "ABC")
            ]),

        new(Input: "ABC"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine(1, "ABC")
            ]),

        new(Input: "ABC"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine(1, "ABC")
            ]),

        new(Input: "ABC\nDEF\nG"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine(1, "ABC"),
                new TextLine(2, "DEF"),
                new TextLine(3, "G")
            ]),

        new(Input: "ABC"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine(1, "ABC")
            ]),

        new(
            Input: Encoding.ASCII.GetBytes(new string('A', 10_000)),
            ExpectedElements:
            [
                new TextLine(1, new string('A', 10_000))
            ])
    ];

    [Theory]
    [MemberData(nameof(TextScenarios))]
    public async Task EscPos_Text_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
