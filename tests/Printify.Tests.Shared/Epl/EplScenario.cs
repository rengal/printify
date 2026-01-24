namespace Printify.Tests.Shared.Epl;

using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Tests.Shared;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;

/// <summary>
/// Represents a deterministic EPL parser scenario consisting of an input payload
/// and the elements that must be produced after parsing it.
/// </summary>
public sealed record EplScenario : ITestScenario
{
    public EplScenario(
        int id,
        byte[] input,
        IReadOnlyList<Command> expectedRequestCommands,
        IReadOnlyList<Command>? expectedPersistedCommands = null,
        IReadOnlyList<CanvasElementDto>? expectedCanvasElements = null)
    {
        Id = id;
        Input = input;
        ExpectedRequestCommands = expectedRequestCommands;
        ExpectedPersistedCommands = expectedPersistedCommands;
        ExpectedCanvasElements = expectedCanvasElements ?? [];
    }

    public override string ToString()
    {
        return $"Id={Id}";
    }

    public int Id { get; }
    public byte[] Input { get; }
    public IReadOnlyList<Command> ExpectedRequestCommands { get; }
    public IReadOnlyList<Command>? ExpectedPersistedCommands { get; }
    public IReadOnlyList<CanvasElementDto> ExpectedCanvasElements { get; }
    public Protocol Protocol => Protocol.Epl;
}
