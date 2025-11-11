namespace Printify.Tests.Shared.EscPos;

using Printify.Domain.Documents.Elements;
using Xunit;

/// <summary>
/// Provides reusable ESC/POS parser scenarios for unit and integration tests.
/// </summary>
public static class EscPosScenarioData
{
    public static TheoryData<EscPosScenario> BellScenarios { get; } =
    [
        new EscPosScenario([0x07], [new Bell()]),
        new EscPosScenario(
            Enumerable.Repeat((byte)0x07, 10).ToArray(),
            Enumerable.Range(0, 10).Select(_ => new Bell()).ToArray())
    ];
}
