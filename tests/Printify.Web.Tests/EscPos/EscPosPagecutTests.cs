using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public class EscPosPagecutTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    public static TheoryData<EscPosScenario> PagecutScenarios =>
    [
        // Function A: Cuts the paper (no feed parameter)
        new(
            Input: [Esc, (byte)'i'],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.PartialOnePoint)
            ]),
        new(
            Input: [Gs, 0x56, 0x00],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Full)
            ]),
        new(
            Input: [Gs, 0x56, 0x30],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Full)
            ]),
        new(
            Input: [Gs, 0x56, 0x01],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Partial)
            ]),
        new(
            Input: [Gs, 0x56, 0x31],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Partial)
            ]),
        // Function B: Feeds paper and cuts (has feed parameter n)
        new(
            Input: [Gs, 0x56, 0x41, 0x05],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Full, 0x05)
            ]),
        new(
            Input: [Gs, 0x56, 0x42, 0x20],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Partial, 0x20)
            ]),
        // Function C: Sets cutting position (has feed parameter n)
        new(
            Input: [Gs, 0x56, 0x61, 0x05],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Full, 0x05)
            ]),
        new(
            Input: [Gs, 0x56, 0x62, 0x20],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Partial, 0x20)
            ]),
        // Function D: Feeds, cuts, and feeds to print start (has feed parameter n)
        new(
            Input: [Gs, 0x56, 0x67, 0x05],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Full, 0x05)
            ]),
        new(
            Input: [Gs, 0x56, 0x68, 0x20],
            ExpectedElements:
            [
                new Pagecut(PagecutMode.Partial, 0x20)
            ]),
    ];

    [Theory]
    [MemberData(nameof(PagecutScenarios))]
    public async Task EscPos_Pagecut_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}

