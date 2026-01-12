namespace Printify.Tests.Shared.Epl;

using Printify.Domain.Documents.Elements;
using Printify.Web.Contracts.Documents.Responses.View.Elements;

/// <summary>
/// Represents a deterministic EPL parser scenario consisting of an input payload
/// and the elements that must be produced after parsing it.
/// </summary>
public sealed record EplScenario
{
    public EplScenario(
        int id,
        byte[] input,
        IReadOnlyList<Element> expectedRequestElements,
        IReadOnlyList<Element>? expectedPersistedElements = null,
        IReadOnlyList<ViewElementDto>? expectedViewElements = null)
    {
        Id = id;
        Input = input;
        ExpectedRequestElements = expectedRequestElements;
        ExpectedPersistedElements = expectedPersistedElements;
        ExpectedViewElements = expectedViewElements ?? [];
    }

    public override string ToString()
    {
        return $"Id={Id}";
    }

    public int Id { get; }
    public byte[] Input { get; }
    public IReadOnlyList<Element> ExpectedRequestElements { get; }
    public IReadOnlyList<Element>? ExpectedPersistedElements { get; }
    public IReadOnlyList<ViewElementDto> ExpectedViewElements { get; }
}
