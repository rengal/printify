using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;

namespace Printify.Tests.Shared;

/// <summary>
/// Interface for test scenarios that can be used across different printer protocols.
/// </summary>
public interface ITestScenario
{
    int Id { get; }
    byte[] Input { get; }
    IReadOnlyList<Command> ExpectedRequestCommands { get; }
    IReadOnlyList<Command>? ExpectedPersistedCommands { get; }
    // Array of arrays: outer array = list of canvases, inner array = list of elements inside each canvas
    IReadOnlyList<IReadOnlyList<CanvasElementDto>> ExpectedCanvasElements { get; }
    Protocol Protocol { get; }

    // Override ToString to show scenario ID in test results
    string ToString();
}
