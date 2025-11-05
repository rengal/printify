using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;

namespace Printify.Web.Tests.EscPos;

public class EscPosPulseTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    public static TheoryData<EscPosScenario> PulseScenarios =>
    [
        new(
            Input: [Esc, (byte)'p', 0x01, 0x05, 0x0A],
            ExpectedElements:
            [
                new Pulse(1, PulsePin.Drawer2, 10, 20)
            ]),
        new(
            Input: [Esc, (byte)'p', 0x00, 0x03, 0x06],
            ExpectedElements:
            [
                new Pulse(1, PulsePin.Drawer1, 6, 12)
            ]),
        new(
            Input:
            [
                Esc, (byte)'p', 0x00, 0x04, 0x08,
                Esc, (byte)'p', 0x01, 0x02, 0x03
            ],
            ExpectedElements:
            [
                new Pulse(1, PulsePin.Drawer1, 8, 16),
                new Pulse(2, PulsePin.Drawer2, 4, 6)
            ])
    ];

    [Theory]
    [MemberData(nameof(PulseScenarios))]
    public async Task EscPos_Pulse_Scenarios_ProduceExpectedDocuments(EscPosScenario scenario)
    {
        await RunScenarioAsync(scenario);
    }
}
