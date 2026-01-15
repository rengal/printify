using Printify.Domain.Documents.Elements;
using Printify.Web.Contracts.Documents.Responses.View.Elements;

namespace Printify.Tests.Shared;

/// <summary>
/// Interface for test scenarios that can be used across different printer protocols.
/// </summary>
public interface ITestScenario
{
    int Id { get; }
    byte[] Input { get; }
    IReadOnlyList<Element> ExpectedRequestElements { get; }
    IReadOnlyList<Element>? ExpectedPersistedElements { get; }
    IReadOnlyList<ViewElementDto> ExpectedViewElements { get; }

    // Override ToString to show scenario ID in test results
    string ToString();
}
