using System.Text;

namespace Printify.Tests.Shared.EscPos;

using Domain.Documents.Elements;
using Xunit;

/// <summary>
/// Provides reusable ESC/POS parser scenarios for unit and integration tests.
/// </summary>
public static class EscPosScenarioData
{
    public static TheoryData<EscPosScenario> BellScenarios { get; } =
    [
        new(Input: [0x07],
            ExpectedElements: [new Bell()]),
        new(
            Input: Enumerable.Repeat((byte)0x07, 10).ToArray(),
            ExpectedElements: Enumerable.Range(0, 10).Select(_ => new Bell()).ToArray())
    ];

    public static TheoryData<EscPosScenario> TextScenarios =>
    [
        new(Input: "A"u8.ToArray(),
            ExpectedElements:
            [
                new TextLine("A")
            ]),
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
            ]),


        new(Input: [.. "ABC"u8, 0x07],
            ExpectedElements:
            [
                new TextLine("ABC"),
                new Bell()
            ]),

        new(Input: [.. "ABC"u8, 0x07, .."DEF"u8, 0x07],
        ExpectedElements:
        [
            new TextLine("ABC"),
            new Bell(),
            new TextLine("DEF"),
            new Bell()
        ]),

        new(Input: [.. "ABC"u8, 0x07, .."DEF\n"u8, 0x07],
            ExpectedElements:
            [
                new TextLine("ABC"),
                new Bell(),
                new TextLine("DEF"),
                new Bell()
            ])
    ];
}
